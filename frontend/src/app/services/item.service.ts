import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, map, Observable, of, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { FALLBACK_CATEGORIES, FALLBACK_LOCATIONS } from '../models/catalog';
import {
  Item,
  ItemPayload,
  ItemQuery,
  ItemStatus,
  oppositeKind,
  sameCampusField,
  StatusEvent,
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

    return this.http.get<unknown>(`${this.base}/items`, { params }).pipe(
      map((body) => this.normalizeItems(body)),
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

  updateStatus(id: number, status: ItemStatus): Observable<Item> {
    return this.http.patch<unknown>(`${this.base}/items/${id}/status`, { status }).pipe(
      map((body) => {
        if (this.isEmptyBody(body)) {
          return this.normalizeItem({ id, status });
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
      map((body) => this.catalogOrFallback(body, FALLBACK_CATEGORIES)),
      catchError(() => of([...FALLBACK_CATEGORIES])),
    );
  }

  locations(): Observable<string[]> {
    return this.http.get<unknown>(`${this.base}/locations`).pipe(
      map((body) => this.catalogOrFallback(body, FALLBACK_LOCATIONS)),
      catchError(() => of([...FALLBACK_LOCATIONS])),
    );
  }

  matches(item: Item): Observable<Item[]> {
    return this.http.get<unknown>(`${this.base}/items/${item.id}/matches`).pipe(
      map((body) => this.normalizeItems(body).filter((candidate) => candidate.id !== item.id)),
      catchError(() => this.fallbackMatches(item)),
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
          (body as { error?: string; title?: string; detail?: string; message?: string }).error ||
          (body as { detail?: string }).detail ||
          (body as { title?: string }).title ||
          (body as { message?: string }).message;
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

  private fallbackMatches(item: Item): Observable<Item[]> {
    return this.list({
      kind: oppositeKind(item.kind),
      category: item.category,
      location: item.location,
      status: 'open',
    }).pipe(
      map((items) =>
        items.filter(
          (candidate) =>
            candidate.id !== item.id &&
            candidate.status === 'open' &&
            candidate.kind === oppositeKind(item.kind) &&
            sameCampusField(candidate.category, item.category) &&
            sameCampusField(candidate.location, item.location),
        ),
      ),
      catchError(() => of([])),
    );
  }

  private catalogOrFallback(body: unknown, fallback: readonly string[]): string[] {
    const list = this.normalizeStringList(body);
    return list.length ? list : [...fallback];
  }

  private normalizeItems(body: unknown): Item[] {
    const raw = this.unwrapArray(body, ['items', 'matches', 'data', 'value']);
    return raw.map((entry) => this.normalizeItem(entry));
  }

  private normalizeItem(body: unknown, fallback: Partial<Item> = {}): Item {
    const source = this.unwrapObject(body);
    const status = this.normalizeStatus(source['status'], fallback.status);
    const createdAt = String(source['createdAt'] ?? source['created_at'] ?? fallback.createdAt ?? '');
    return {
      id: Number(source['id'] ?? fallback.id ?? 0),
      title: String(source['title'] ?? fallback.title ?? ''),
      description: String(source['description'] ?? fallback.description ?? ''),
      location: String(source['location'] ?? fallback.location ?? ''),
      category: String(source['category'] ?? fallback.category ?? ''),
      contact: String(source['contact'] ?? fallback.contact ?? ''),
      photoUrl: this.optionalString(source['photoUrl'] ?? source['photoURL']),
      kind: source['kind'] === 'found' || fallback.kind === 'found' ? 'found' : 'lost',
      status,
      createdAt,
      statusHistory: this.normalizeStatusHistory(source, fallback.statusHistory),
    };
  }

  private normalizeStatusHistory(
    source: Record<string, unknown>,
    fallback?: StatusEvent[],
  ): StatusEvent[] {
    const raw = source['statusHistory'] ?? source['StatusHistory'];
    if (Array.isArray(raw)) {
      return raw
        .map((entry) => this.normalizeStatusEvent(entry))
        .filter((event): event is StatusEvent => event !== null);
    }
    return fallback ? [...fallback] : [];
  }

  private normalizeStatusEvent(entry: unknown): StatusEvent | null {
    if (!entry || typeof entry !== 'object') {
      return null;
    }
    const obj = entry as Record<string, unknown>;
    const to = this.normalizeStatus(obj['to'] ?? obj['status'] ?? obj['toStatus']);
    const fromRaw = obj['from'] ?? obj['fromStatus'];
    const from =
      fromRaw === 'open' || fromRaw === 'claimed' || fromRaw === 'closed' ? fromRaw : null;
    return {
      from,
      to,
      changedAt: String(obj['changedAt'] ?? obj['at'] ?? obj['createdAt'] ?? ''),
    };
  }

  private normalizeStatus(value: unknown, fallback?: ItemStatus): ItemStatus {
    if (value === 'claimed' || value === 'closed' || value === 'open') {
      return value;
    }
    return fallback ?? 'open';
  }

  private normalizeStringList(body: unknown): string[] {
    const raw = this.unwrapArray(body, ['locations', 'categories', 'data', 'value', 'items']);
    return raw
      .map((entry) => {
        if (typeof entry === 'string') {
          return entry.trim();
        }
        if (entry && typeof entry === 'object') {
          const obj = entry as Record<string, unknown>;
          return String(obj['name'] ?? obj['slug'] ?? obj['title'] ?? obj['value'] ?? '').trim();
        }
        return '';
      })
      .filter(Boolean);
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
