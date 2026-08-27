import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ItemCard } from '../components/item-card';
import { KindBadge, StatusBadge } from '../components/badges';
import {
  isContactVisible,
  Item,
  ItemStatus,
  STATUS_LABELS,
  STATUS_OPTIONS,
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
  readonly matches = signal<Item[]>([]);
  readonly matchesUnavailable = signal(false);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly actionBusy = signal(false);
  readonly confirmDelete = signal(false);
  readonly photoBroken = signal(false);

  readonly statusOptions = STATUS_OPTIONS;
  readonly statusLabels = STATUS_LABELS;
  readonly formatDate = formatDateTr;
  readonly contactVisible = isContactVisible;

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const id = Number(params.get('id'));
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
    this.matches.set([]);
    this.matchesUnavailable.set(false);

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
      next: (updated) => {
        this.item.set({ ...current, ...updated });
        this.actionBusy.set(false);
        this.loadMatches(this.item()!);
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
      next: () => this.router.navigateByUrl('/'),
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

  statusIndex(status: ItemStatus): number {
    return this.statusOptions.indexOf(status);
  }

  private loadMatches(item: Item): void {
    this.api.matches(item).subscribe({
      next: (matches) => {
        this.matches.set(matches);
        this.matchesUnavailable.set(false);
      },
      error: () => {
        this.matches.set([]);
        this.matchesUnavailable.set(true);
      },
    });
  }
}
