import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { ItemCard } from '../components/item-card';
import { FALLBACK_CATEGORIES, FALLBACK_LOCATIONS } from '../models/catalog';
import {
  Item,
  ItemQuery,
  KIND_LABELS,
  KIND_OPTIONS,
  STATUS_LABELS,
  STATUS_OPTIONS,
} from '../models/item';
import { ItemService } from '../services/item.service';

@Component({
  selector: 'app-item-list',
  imports: [ReactiveFormsModule, RouterLink, ItemCard],
  templateUrl: './item-list.html',
  styleUrl: './item-list.css',
})
export class ItemList implements OnInit {
  private readonly api = inject(ItemService);
  private readonly destroyRef = inject(DestroyRef);

  readonly searchControl = new FormControl('', { nonNullable: true });
  readonly kind = signal('');
  readonly status = signal('');
  readonly category = signal('');
  readonly location = signal('');

  readonly items = signal<Item[]>([]);
  readonly categories = signal<string[]>([...FALLBACK_CATEGORIES]);
  readonly locations = signal<string[]>([...FALLBACK_LOCATIONS]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  readonly kindOptions = KIND_OPTIONS;
  readonly statusOptions = STATUS_OPTIONS;
  readonly kindLabels = KIND_LABELS;
  readonly statusLabels = STATUS_LABELS;

  ngOnInit(): void {
    this.searchControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.loadItems());

    this.loadCatalog();
    this.loadItems();
  }

  get hasFilters(): boolean {
    return Boolean(
      this.searchControl.value.trim() ||
        this.kind() ||
        this.status() ||
        this.category() ||
        this.location(),
    );
  }

  setKind(event: Event): void {
    this.kind.set((event.target as HTMLSelectElement).value);
    this.loadItems();
  }

  setStatus(event: Event): void {
    this.status.set((event.target as HTMLSelectElement).value);
    this.loadItems();
  }

  setCategory(event: Event): void {
    this.category.set((event.target as HTMLSelectElement).value);
    this.loadItems();
  }

  setLocation(event: Event): void {
    this.location.set((event.target as HTMLSelectElement).value);
    this.loadItems();
  }

  clearFilters(): void {
    this.searchControl.setValue('', { emitEvent: false });
    this.kind.set('');
    this.status.set('');
    this.category.set('');
    this.location.set('');
    this.loadItems();
  }

  loadItems(): void {
    this.loading.set(true);
    this.error.set(null);

    const query: ItemQuery = {
      q: this.searchControl.value.trim(),
      kind: this.kind(),
      status: this.status(),
      category: this.category(),
      location: this.location(),
    };

    this.api.list(query).subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: (err: Error) => {
        this.error.set(err.message);
        this.items.set([]);
        this.loading.set(false);
      },
    });
  }

  private loadCatalog(): void {
    this.api.categories().subscribe({
      next: (categories) => this.categories.set(categories),
    });
    this.api.locations().subscribe({
      next: (locations) => this.locations.set(locations),
    });
  }
}
