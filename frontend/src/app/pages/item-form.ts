import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { KIND_LABELS, KIND_OPTIONS, ItemKind, ItemPayload, ItemStatus } from '../models/item';
import { ItemService } from '../services/item.service';

@Component({
  selector: 'app-item-form',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './item-form.html',
  styleUrl: './item-form.css',
})
export class ItemForm implements OnInit {
  private readonly api = inject(ItemService);
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly isEdit = signal(false);
  readonly itemId = signal<number | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly categories = signal<string[]>([]);
  readonly useCustomCategory = signal(true);
  readonly existingStatus = signal<ItemStatus>('open');

  readonly kindOptions = KIND_OPTIONS;
  readonly kindLabels = KIND_LABELS;

  readonly form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.minLength(2)]],
    description: ['', Validators.required],
    location: ['', Validators.required],
    category: ['', Validators.required],
    contact: ['', Validators.required],
    photoUrl: [''],
    kind: this.fb.nonNullable.control<ItemKind>('lost', Validators.required),
  });

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    const id = idParam ? Number(idParam) : null;
    this.loadCategories();

    if (id && Number.isFinite(id)) {
      this.isEdit.set(true);
      this.itemId.set(id);
      this.loadItem(id);
    }
  }

  onCategorySelect(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    if (value === '__custom__') {
      this.useCustomCategory.set(true);
      this.form.controls.category.setValue('');
      return;
    }

    this.useCustomCategory.set(false);
    this.form.controls.category.setValue(value);
  }

  submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.saving()) {
      return;
    }

    const value = this.form.getRawValue();
    const payload: ItemPayload = {
      title: value.title.trim(),
      description: value.description.trim(),
      location: value.location.trim(),
      category: value.category.trim(),
      contact: value.contact.trim(),
      photoUrl: value.photoUrl.trim(),
      kind: value.kind,
      status: this.isEdit() ? this.existingStatus() : 'open',
    };

    this.saving.set(true);
    this.error.set(null);

    const request = this.isEdit()
      ? this.api.update(this.itemId()!, payload)
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

  private loadCategories(): void {
    this.api.categories().subscribe({
      next: (categories) => {
        this.categories.set(categories);
        this.syncCategoryMode();
      },
      error: () => {
        this.categories.set([]);
        this.useCustomCategory.set(true);
      },
    });
  }

  private loadItem(id: number): void {
    this.loading.set(true);
    this.error.set(null);

    this.api.get(id).subscribe({
      next: (item) => {
        this.existingStatus.set(item.status);
        this.form.patchValue({
          title: item.title,
          description: item.description,
          location: item.location,
          category: item.category,
          contact: item.contact,
          photoUrl: item.photoUrl ?? '',
          kind: item.kind,
        });
        this.syncCategoryMode(item.category);
        this.loading.set(false);
      },
      error: (err: Error) => {
        this.error.set(err.message);
        this.loading.set(false);
      },
    });
  }

  private syncCategoryMode(currentCategory?: string): void {
    const category = currentCategory ?? this.form.controls.category.value;
    const list = this.categories();
    if (!list.length) {
      this.useCustomCategory.set(true);
      return;
    }
    if (!category) {
      this.useCustomCategory.set(false);
      return;
    }
    this.useCustomCategory.set(!list.includes(category));
  }
}
