import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink, RouterLinkActive } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { ItemCard } from '../components/item-card';
import {
  BoardFlow,
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
  imports: [ReactiveFormsModule, RouterLink, RouterLinkActive, ItemCard],
  templateUrl: './item-list.html',
  styleUrl: './item-list.css',
})
export class ItemList implements OnInit {
  private readonly api = inject(ItemService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  readonly searchControl = new FormControl('', { nonNullable: true });
  readonly flow = signal<BoardFlow>('all');
  readonly kind = signal('');
  readonly status = signal('');
  readonly category = signal('');
  readonly location = signal('');

  readonly items = signal<Item[]>([]);
  readonly categories = signal<string[]>([]);
  readonly locations = signal<string[]>([]);
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

    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((data) => {
      const flow = (data['flow'] as BoardFlow | undefined) ?? 'all';
      this.flow.set(flow);
      this.kind.set(flow === 'all' ? '' : flow);
      this.loadItems();
    });

    this.loadLookups();
  }

  get hasFilters(): boolean {
    const flowLockedKind = this.flow() !== 'all';
    return Boolean(
      this.searchControl.value.trim() ||
        (!flowLockedKind && this.kind()) ||
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
    this.status.set('');
    this.category.set('');
    this.location.set('');
    this.kind.set(this.flow() === 'all' ? '' : this.flow());
    this.loadItems();
  }

  loadItems(): void {
    this.loading.set(true);
    this.error.set(null);

    const flow = this.flow();
    const query: ItemQuery = {
      q: this.searchControl.value.trim(),
      kind: flow === 'all' ? this.kind() : flow,
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

  private loadLookups(): void {
    this.api.categories().subscribe({
      next: (categories) => this.categories.set(categories),
    });
    this.api.locations().subscribe({
      next: (locations) => this.locations.set(locations),
    });
  }
}
