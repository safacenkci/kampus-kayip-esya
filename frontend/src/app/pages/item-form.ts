import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  ensureCurrentLookup,
  FALLBACK_CATEGORIES,
  FALLBACK_LOCATIONS,
  ItemKind,
  ItemPayload,
  ItemStatus,
} from '../models/item';
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
  readonly kind = signal<ItemKind>('lost');
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly categories = signal<string[]>([...FALLBACK_CATEGORIES]);
  readonly locations = signal<string[]>([...FALLBACK_LOCATIONS]);
  readonly existingStatus = signal<ItemStatus>('open');
  readonly existingContact = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.minLength(2)]],
    description: ['', Validators.required],
    location: ['', Validators.required],
    category: ['', Validators.required],
    contact: ['', Validators.required],
    photoUrl: [''],
  });

  ngOnInit(): void {
    const kindFromRoute = this.route.snapshot.data['kind'] as ItemKind | undefined;
    if (kindFromRoute === 'lost' || kindFromRoute === 'found') {
      this.kind.set(kindFromRoute);
    }

    const idParam = this.route.snapshot.paramMap.get('id');
    const id = idParam ? Number(idParam) : null;
    this.loadLookups();

    if (id && Number.isFinite(id)) {
      this.isEdit.set(true);
      this.itemId.set(id);
      this.loadItem(id);
    }
  }

  get backLink(): string {
    if (this.isEdit() && this.itemId()) {
      return `/ilan/${this.itemId()}`;
    }
    return this.kind() === 'found' ? '/buldum' : '/kaybettim';
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
      kind: this.kind(),
      status: this.isEdit() ? this.existingStatus() : 'open',
    };

    this.saving.set(true);
    this.error.set(null);

    const request = this.isEdit()
      ? this.api.update(this.itemId()!, payload)
      : this.api.create(payload);

    request.subscribe({
      next: (item) => this.router.navigateByUrl(item.id ? `/ilan/${item.id}` : this.backLink),
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

  private loadLookups(): void {
    this.api.categories().subscribe({
      next: (categories) =>
        this.categories.set(ensureCurrentLookup(categories, this.form.controls.category.value)),
    });
    this.api.locations().subscribe({
      next: (locations) =>
        this.locations.set(ensureCurrentLookup(locations, this.form.controls.location.value)),
    });
  }

  private loadItem(id: number): void {
    this.loading.set(true);
    this.error.set(null);

    this.api.get(id).subscribe({
      next: (item) => {
        this.kind.set(item.kind);
        this.existingStatus.set(item.status);
        this.existingContact.set(item.contact);
        this.form.patchValue({
          title: item.title,
          description: item.description,
          location: item.location,
          category: item.category,
          contact: item.contact ?? '',
          photoUrl: item.photoUrl ?? '',
        });
        if (item.status === 'open' && !item.contact) {
          this.form.controls.contact.clearValidators();
          this.form.controls.contact.updateValueAndValidity();
        }
        this.categories.update((list) => ensureCurrentLookup(list, item.category));
        this.locations.update((list) => ensureCurrentLookup(list, item.location));
        this.loading.set(false);
      },
      error: (err: Error) => {
        this.error.set(err.message);
        this.loading.set(false);
      },
    });
  }
}
