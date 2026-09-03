import { Component, computed, inject, input, OnInit, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CategoryIcon } from '../components/category-icon';
import { ItemCard } from '../components/item-card';
import { FALLBACK_CATEGORIES, FALLBACK_LOCATIONS, mergeCatalog } from '../models/catalog';
import { flowFor } from '../models/flows';
import { Item, ItemKind, ItemPayload, ItemStatus } from '../models/item';
import { ItemService } from '../services/item.service';

const TOTAL_STEPS = 3;

@Component({
  selector: 'app-notice-wizard',
  imports: [ReactiveFormsModule, RouterLink, CategoryIcon, ItemCard],
  templateUrl: './notice-wizard.html',
  styleUrl: './notice-wizard.css',
})
export class NoticeWizard implements OnInit {
  private readonly api = inject(ItemService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);

  readonly kind = input.required<ItemKind>();
  readonly editId = input<number | null>(null);

  readonly copy = computed(() => flowFor(this.kind()));
  readonly step = signal(1);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly locations = signal<string[]>([...FALLBACK_LOCATIONS]);
  readonly categories = signal<string[]>([...FALLBACK_CATEGORIES]);
  readonly existingStatus = signal<ItemStatus>('open');

  readonly form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.minLength(2)]],
    description: ['', Validators.required],
    location: ['', Validators.required],
    category: ['', Validators.required],
    contact: ['', Validators.required],
    photoUrl: [''],
  });

  readonly totalSteps = TOTAL_STEPS;

  /** İlerleme yüzdesi — üstteki şerit için. */
  readonly progress = computed(() => Math.round((this.step() / TOTAL_STEPS) * 100));

  private readonly formValue = toSignal(this.form.valueChanges, {
    initialValue: this.form.getRawValue(),
  });

  /** Form doldukça güncellenen canlı ilan önizlemesi. */
  readonly preview = computed<Item>(() => {
    const value = this.formValue();
    const photo = (value.photoUrl ?? '').trim();
    return {
      id: this.editId() ?? 0,
      title: (value.title ?? '').trim() || 'Başlık henüz yazılmadı',
      description: (value.description ?? '').trim(),
      location: value.location ?? '',
      category: value.category ?? '',
      contact: (value.contact ?? '').trim(),
      photoUrl: photo || null,
      kind: this.kind(),
      status: this.editId() ? this.existingStatus() : 'open',
      createdAt: new Date().toISOString(),
      statusHistory: [],
    };
  });

  /** Etiketten seçim: aynı değere tekrar basmak seçimi bırakmaz, alan zorunludur. */
  pick(field: 'category' | 'location', value: string): void {
    this.form.controls[field].setValue(value);
    this.form.controls[field].markAsTouched();
  }

  isPicked(field: 'category' | 'location', value: string): boolean {
    return this.form.controls[field].value === value;
  }

  ngOnInit(): void {
    const copy = this.copy();
    this.form.patchValue({
      location: copy.defaultLocation,
      category: copy.defaultCategory,
    });
    this.loadCatalog();

    const id = this.editId();
    if (id) {
      this.loadItem(id);
    }
  }

  next(): void {
    if (!this.validateStep(this.step())) {
      return;
    }
    this.step.update((value) => Math.min(3, value + 1));
  }

  back(): void {
    this.step.update((value) => Math.max(1, value - 1));
  }

  goTo(step: number): void {
    if (step < this.step() || this.validateStep(this.step())) {
      this.step.set(step);
    }
  }

  submit(): void {
    if (!this.validateStep(1) || !this.validateStep(2) || !this.validateStep(3) || this.saving()) {
      this.step.set(this.firstInvalidStep());
      return;
    }

    const value = this.form.getRawValue();
    const payload: ItemPayload = {
      title: value.title.trim(),
      description: value.description.trim(),
      location: value.location,
      category: value.category,
      contact: value.contact.trim(),
      photoUrl: value.photoUrl.trim(),
      kind: this.kind(),
      status: this.editId() ? this.existingStatus() : 'open',
    };

    this.saving.set(true);
    this.error.set(null);

    const request = this.editId()
      ? this.api.update(this.editId()!, payload)
      : this.api.create(payload);

    request.subscribe({
      next: (item) => this.router.navigateByUrl(item.id ? `/${item.id}` : '/'),
      error: (err: Error) => {
        this.error.set(err.message);
        this.saving.set(false);
      },
    });
  }

  invalid(controlName: keyof typeof this.form.controls): boolean {
    const control = this.form.controls[controlName];
    return control.invalid && control.touched;
  }

  private validateStep(step: number): boolean {
    const fields =
      step === 1
        ? (['title', 'category', 'description'] as const)
        : step === 2
          ? (['location'] as const)
          : (['contact'] as const);

    let ok = true;
    for (const field of fields) {
      const control = this.form.controls[field];
      control.markAsTouched();
      if (control.invalid) {
        ok = false;
      }
    }
    return ok;
  }

  private firstInvalidStep(): number {
    if (!this.validateStep(1)) {
      return 1;
    }
    if (!this.validateStep(2)) {
      return 2;
    }
    return 3;
  }

  private loadCatalog(): void {
    this.api.locations().subscribe({
      next: (locations) => this.locations.set(locations),
      error: () => this.locations.set([]),
    });
    this.api.categories().subscribe({
      next: (categories) => this.categories.set(categories),
      error: () => this.categories.set([]),
    });
  }

  private loadItem(id: number): void {
    this.loading.set(true);
    this.error.set(null);

    this.api.get(id).subscribe({
      next: (item) => this.applyItem(item),
      error: (err: Error) => {
        this.error.set(err.message);
        this.loading.set(false);
      },
    });
  }

  private applyItem(item: Item): void {
    this.existingStatus.set(item.status);
    if (item.location) {
      this.locations.update((list) => mergeCatalog(list, [item.location]));
    }
    if (item.category) {
      this.categories.update((list) => mergeCatalog(list, [item.category]));
    }
    this.form.patchValue({
      title: item.title,
      description: item.description,
      location: item.location,
      category: item.category,
      contact: item.contact,
      photoUrl: item.photoUrl ?? '',
    });
    this.loading.set(false);
  }
}
