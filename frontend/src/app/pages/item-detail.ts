import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { KindBadge, StatusBadge } from '../components/badges';
import { ItemCard } from '../components/item-card';
import {
  canShowContact,
  Item,
  ItemStatus,
  STATUS_HISTORY_LABELS,
  STATUS_LABELS,
  STATUS_OPTIONS,
  StatusHistoryEntry,
} from '../models/item';
import { ItemService } from '../services/item.service';
import { formatDateTr } from '../utils/format';

@Component({
  selector: 'app-item-detail',
  imports: [RouterLink, KindBadge, StatusBadge, ItemCard],
  templateUrl: './item-detail.html',
  styleUrl: './item-detail.css',
})
export class ItemDetail implements OnInit {
  private readonly api = inject(ItemService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly item = signal<Item | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly actionBusy = signal(false);
  readonly confirmDelete = signal(false);
  readonly photoBroken = signal(false);
  readonly matches = signal<Item[]>([]);
  readonly matchesLoading = signal(false);
  readonly matchesError = signal<string | null>(null);
  readonly matchesFromApi = signal(false);
  readonly sessionHistory = signal<StatusHistoryEntry[]>([]);

  readonly statusOptions = STATUS_OPTIONS;
  readonly statusLabels = STATUS_LABELS;
  readonly formatDate = formatDateTr;
  readonly canShowContact = canShowContact;

  readonly timeline = computed(() => {
    const item = this.item();
    if (!item) {
      return [];
    }
    if (item.statusHistory.length) {
      return item.statusHistory;
    }
    return [{ status: 'open' as const, at: item.createdAt }, ...this.sessionHistory()];
  });

  readonly boardLink = computed(() => {
    const item = this.item();
    if (!item) {
      return '/';
    }
    return item.kind === 'found' ? '/buldum' : '/kaybettim';
  });

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const id = Number(params.get('id'));
      this.sessionHistory.set([]);
      this.load(id);
    });
  }

  load(id = Number(this.route.snapshot.paramMap.get('id'))): void {
    if (!Number.isFinite(id) || id <= 0) {
      this.loading.set(false);
      this.error.set('Geçersiz ilan.');
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.actionError.set(null);
    this.confirmDelete.set(false);
    this.photoBroken.set(false);

    this.api.get(id).subscribe({
      next: (item) => {
        this.item.set(item);
        this.loading.set(false);
        this.loadMatches(item);
      },
      error: (err: Error) => {
        this.item.set(null);
        this.error.set(err.message);
        this.loading.set(false);
      },
    });
  }

  changeStatus(status: ItemStatus): void {
    const current = this.item();
    if (!current || current.status === status || this.actionBusy()) {
      return;
    }

    this.actionBusy.set(true);
    this.actionError.set(null);

    this.api.updateStatus(current.id, status).subscribe({
      next: () => {
        this.sessionHistory.update((events) => [
          ...events,
          { status, at: new Date().toISOString() },
        ]);
        this.actionBusy.set(false);
        this.reloadAfterStatus(current.id);
      },
      error: (err: Error) => {
        this.actionError.set(err.message);
        this.actionBusy.set(false);
      },
    });
  }

  deleteItem(): void {
    const current = this.item();
    if (!current || this.actionBusy()) {
      return;
    }

    this.actionBusy.set(true);
    this.actionError.set(null);

    this.api.delete(current.id).subscribe({
      next: () => this.router.navigateByUrl(this.boardLink()),
      error: (err: Error) => {
        this.actionError.set(err.message);
        this.actionBusy.set(false);
        this.confirmDelete.set(false);
      },
    });
  }

  onPhotoError(): void {
    this.photoBroken.set(true);
  }

  contactHref(contact: string): string | null {
    if (contact.includes('@')) {
      return `mailto:${contact}`;
    }
    if (/^[+\d][\d\s()-]{5,}$/.test(contact)) {
      return `tel:${contact.replace(/\s/g, '')}`;
    }
    return null;
  }

  historyLabel(status: ItemStatus): string {
    return STATUS_HISTORY_LABELS[status];
  }

  private reloadAfterStatus(id: number): void {
    this.api.get(id).subscribe({
      next: (item) => {
        this.item.set(item);
        this.loadMatches(item);
      },
      error: () => {
        const current = this.item();
        if (current) {
          this.loadMatches(current);
        }
      },
    });
  }

  retryMatches(): void {
    const item = this.item();
    if (item) {
      this.loadMatches(item);
    }
  }

  private loadMatches(item: Item): void {
    this.matchesLoading.set(true);
    this.matchesError.set(null);
    this.matches.set([]);
    this.matchesFromApi.set(false);

    this.api.matches(item.id).subscribe({
      next: (result) => {
        if (result) {
          this.matches.set(result);
          this.matchesFromApi.set(true);
          this.matchesLoading.set(false);
          return;
        }
        this.loadClientMatches(item);
      },
      error: (err: Error) => {
        this.matchesError.set(err.message);
        this.matchesLoading.set(false);
      },
    });
  }

  private loadClientMatches(item: Item): void {
    this.api
      .list({
        status: 'open',
        category: item.category,
        location: item.location,
      })
      .subscribe({
        next: (items) => {
          this.matches.set(this.api.clientMatches(item, items));
          this.matchesFromApi.set(false);
          this.matchesLoading.set(false);
        },
        error: (err: Error) => {
          this.matchesError.set(err.message);
          this.matchesLoading.set(false);
        },
      });
  }
}
