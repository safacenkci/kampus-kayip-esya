import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { CategoryIcon } from '../components/category-icon';
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
  imports: [ReactiveFormsModule, RouterLink, ItemCard, CategoryIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
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
  readonly searchTerm = signal('');

  readonly items = signal<Item[]>([]);
  readonly categories = signal<string[]>([...FALLBACK_CATEGORIES]);
  readonly locations = signal<string[]>([...FALLBACK_LOCATIONS]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  readonly kindOptions = KIND_OPTIONS;
  readonly statusOptions = STATUS_OPTIONS;
  readonly kindLabels = KIND_LABELS;
  readonly statusLabels = STATUS_LABELS;

  /** Listedeki ilanların tür kırılımı — başlıktaki sayaç şeridi için. */
  readonly counts = computed(() => {
    const all = this.items();
    return {
      total: all.length,
      lost: all.filter((item) => item.kind === 'lost').length,
      found: all.filter((item) => item.kind === 'found').length,
    };
  });

  readonly activeFilterCount = computed(() => {
    return [this.kind(), this.status(), this.category(), this.location(), this.searchTerm()].filter(
      (value) => value.trim().length > 0,
    ).length;
  });

  readonly hasFilters = computed(() => this.activeFilterCount() > 0);

  ngOnInit(): void {
    this.searchControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((value) => {
        this.searchTerm.set(value.trim());
        this.loadItems();
      });

    this.loadCatalog();
    this.loadItems();
  }

  toggleKind(value: string): void {
    this.kind.set(this.kind() === value ? '' : value);
    this.loadItems();
  }

  toggleStatus(value: string): void {
    this.status.set(this.status() === value ? '' : value);
    this.loadItems();
  }

  toggleCategory(value: string): void {
    this.category.set(this.category() === value ? '' : value);
    this.loadItems();
  }

  toggleLocation(value: string): void {
    this.location.set(this.location() === value ? '' : value);
    this.loadItems();
  }

  clearFilters(): void {
    this.searchControl.setValue('', { emitEvent: false });
    this.searchTerm.set('');
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
