import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, map, Observable, of, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  FALLBACK_CATEGORIES,
  FALLBACK_LOCATIONS,
  Item,
  ItemPayload,
  ItemQuery,
  ItemStatus,
  mergeLookups,
  sameLookup,
  StatusHistoryEntry,
} from '../models/item';

@Injectable({ providedIn: 'root' })
export class ItemService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  list(query: ItemQuery = {}): Observable<Item[]> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(query)) {
      if (value) {
        params = params.set(key, value);
      }
    }

    const requestedLocation = query.location?.trim() ?? '';

    return this.http.get<unknown>(`${this.base}/items`, { params }).pipe(
      map((body) => {
        const items = this.normalizeItems(body);
        if (!requestedLocation) {
          return items;
        }
        return items.filter((item) => sameLookup(item.location, requestedLocation));
      }),
      catchError((err) => throwError(() => this.toAppError(err, 'İlanlar yüklenemedi.'))),
    );
  }

  get(id: number): Observable<Item> {
    return this.http.get<unknown>(`${this.base}/items/${id}`).pipe(
      map((body) => this.normalizeItem(body)),
      catchError((err) =>
        throwError(() =>
          this.toAppError(err, err.status === 404 ? 'İlan bulunamadı.' : 'İlan yüklenemedi.'),
        ),
      ),
    );
  }

  create(payload: ItemPayload): Observable<Item> {
    return this.http.post<unknown>(`${this.base}/items`, payload).pipe(
      map((body) => this.normalizeItem(body)),
      catchError((err) => throwError(() => this.toAppError(err, 'İlan oluşturulamadı.'))),
    );
  }

  update(id: number, payload: ItemPayload): Observable<Item> {
    return this.http.put<unknown>(`${this.base}/items/${id}`, payload).pipe(
      map((body) => this.normalizeItem(body)),
      catchError((err) => throwError(() => this.toAppError(err, 'İlan güncellenemedi.'))),
    );
  }

  updateStatus(id: number, status: ItemStatus): Observable<Partial<Item>> {
    return this.http.patch<unknown>(`${this.base}/items/${id}/status`, { status }).pipe(
      map((body) => {
        if (this.isEmptyBody(body)) {
          return { id, status };
        }
        return this.normalizeItem(body, { id, status });
      }),
      catchError((err) => throwError(() => this.toAppError(err, 'Durum güncellenemedi.'))),
    );
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/items/${id}`).pipe(
      catchError((err) => throwError(() => this.toAppError(err, 'İlan silinemedi.'))),
    );
  }

  categories(): Observable<string[]> {
    return this.http.get<unknown>(`${this.base}/categories`).pipe(
      map((body) =>
        mergeLookups(this.normalizeNamedList(body, ['categories', 'data', 'value', 'items']), FALLBACK_CATEGORIES),
      ),
      catchError(() => of([...FALLBACK_CATEGORIES])),
    );
  }

  locations(): Observable<string[]> {
    return this.http.get<unknown>(`${this.base}/locations`).pipe(
      map((body) =>
        mergeLookups(this.normalizeNamedList(body, ['locations', 'data', 'value', 'items']), FALLBACK_LOCATIONS),
      ),
      catchError(() => of([...FALLBACK_LOCATIONS])),
    );
  }

  matches(id: number): Observable<Item[] | null> {
    return this.http.get<unknown>(`${this.base}/items/${id}/matches`).pipe(
      map((body) => this.normalizeItems(body)),
      catchError((err) => {
        if (this.isMissingEndpoint(err)) {
          return of(null);
        }
        return throwError(() => this.toAppError(err, 'Eşleşmeler yüklenemedi.'));
      }),
    );
  }

  clientMatches(item: Item, candidates: Item[]): Item[] {
    return candidates.filter(
      (candidate) =>
        candidate.id !== item.id &&
        candidate.status === 'open' &&
        candidate.kind !== item.kind &&
        sameLookup(candidate.category, item.category) &&
        sameLookup(candidate.location, item.location),
    );
  }

  toAppError(err: unknown, fallback: string): Error {
    if (err instanceof HttpErrorResponse) {
      if (err.status === 0) {
        return new Error(
          'Sunucuya bağlanılamadı. API’nin http://localhost:5080 adresinde çalıştığından emin olun.',
        );
      }

      const body = err.error;
      if (typeof body === 'string' && body.trim()) {
        return new Error(body);
      }
      if (body && typeof body === 'object') {
        const message =
          (body as { title?: string; detail?: string; message?: string; error?: string }).detail ||
          (body as { title?: string; message?: string }).title ||
          (body as { message?: string }).message ||
          (body as { error?: string }).error;
        if (message) {
          return new Error(message);
        }
      }
    }

    if (err instanceof Error && err.message) {
      return err;
    }

    return new Error(fallback);
  }

  private normalizeItems(body: unknown): Item[] {
    const raw = this.unwrapArray(body, ['items', 'data', 'value', 'matches']);
    return raw.map((entry) => this.normalizeItem(entry));
  }

  private normalizeItem(body: unknown, fallback: Partial<Item> = {}): Item {
    const source = this.unwrapObject(body);
    const status: ItemStatus =
      source['status'] === 'claimed' || source['status'] === 'closed'
        ? source['status']
        : (fallback.status ?? 'open');

    return {
      id: Number(source['id'] ?? fallback.id ?? 0),
      title: String(source['title'] ?? ''),
      description: String(source['description'] ?? ''),
      location: String(source['location'] ?? ''),
      category: String(source['category'] ?? ''),
      contact: this.readOptionalContact(source, fallback.contact),
      photoUrl: this.optionalString(source['photoUrl'] ?? source['photoURL']),
      kind: source['kind'] === 'found' ? 'found' : 'lost',
      status,
      createdAt: String(source['createdAt'] ?? source['created_at'] ?? fallback.createdAt ?? ''),
      statusHistory: this.normalizeHistory(
        source['statusHistory'] ?? source['status_history'] ?? source['history'],
      ),
    };
  }

  private normalizeHistory(raw: unknown): StatusHistoryEntry[] {
    if (!Array.isArray(raw)) {
      return [];
    }

    return raw
      .map((entry) => {
        if (!entry || typeof entry !== 'object') {
          return null;
        }
        const obj = entry as Record<string, unknown>;
        const statusValue = String(obj['status'] ?? '').toLowerCase();
        const status: ItemStatus | null =
          statusValue === 'open' || statusValue === 'claimed' || statusValue === 'closed'
            ? statusValue
            : null;
        if (!status) {
          return null;
        }
        const at = this.optionalString(
          obj['at'] ?? obj['changedAt'] ?? obj['createdAt'] ?? obj['timestamp'] ?? obj['date'],
        );
        return { status, at };
      })
      .filter((entry): entry is StatusHistoryEntry => entry != null);
  }

  private normalizeNamedList(body: unknown, keys: string[]): string[] {
    const raw = this.unwrapArray(body, keys);
    const names = raw
      .map((entry) => {
        if (typeof entry === 'string') {
          return entry.trim();
        }
        if (entry && typeof entry === 'object') {
          const obj = entry as Record<string, unknown>;
          return String(
            obj['name'] ?? obj['slug'] ?? obj['title'] ?? obj['category'] ?? obj['location'] ?? obj['value'] ?? '',
          ).trim();
        }
        return '';
      })
      .filter(Boolean);

    return [...new Set(names)];
  }

  private readOptionalContact(source: Record<string, unknown>, fallback?: string | null): string | null {
    if ('contact' in source) {
      return this.optionalString(source['contact']);
    }
    if ('Contact' in source) {
      return this.optionalString(source['Contact']);
    }
    return fallback ?? null;
  }

  private isMissingEndpoint(err: unknown): boolean {
    return err instanceof HttpErrorResponse && (err.status === 404 || err.status === 405 || err.status === 501);
  }

  private isEmptyBody(body: unknown): boolean {
    return (
      body == null ||
      body === '' ||
      (typeof body === 'object' && !Array.isArray(body) && Object.keys(body as object).length === 0)
    );
  }

  private unwrapArray(body: unknown, keys: string[]): unknown[] {
    if (Array.isArray(body)) {
      return body;
    }
    if (body && typeof body === 'object') {
      const obj = body as Record<string, unknown>;
      for (const key of keys) {
        if (Array.isArray(obj[key])) {
          return obj[key] as unknown[];
        }
      }
    }
    return [];
  }

  private unwrapObject(body: unknown): Record<string, unknown> {
    if (body && typeof body === 'object' && !Array.isArray(body)) {
      const obj = body as Record<string, unknown>;
      const nested = obj['item'] ?? obj['data'] ?? obj['value'];
      if (nested && typeof nested === 'object' && !Array.isArray(nested)) {
        return nested as Record<string, unknown>;
      }
      return obj;
    }
    return {};
  }

  private optionalString(value: unknown): string | null {
    if (value == null) {
      return null;
    }
    const text = String(value).trim();
    return text ? text : null;
  }
}
