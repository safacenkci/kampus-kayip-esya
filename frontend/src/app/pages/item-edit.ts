import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ItemService } from '../services/item.service';
import { BuldumPage } from './buldum';
import { KaybettimPage } from './kaybettim';

@Component({
  selector: 'app-item-edit',
  imports: [RouterLink, KaybettimPage, BuldumPage],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (loading()) {
      <div class="skeleton panel-wait"></div>
    } @else if (error()) {
      <div class="banner banner-error" role="alert">
        <p>{{ error() }}</p>
        <a routerLink="/" class="btn btn-ghost btn-sm">Panoya dön</a>
      </div>
    } @else if (kind() === 'found') {
      <app-buldum [editId]="itemId()" />
    } @else {
      <app-kaybettim [editId]="itemId()" />
    }
  `,
  styles: `
    .panel-wait {
      height: 320px;
      border-radius: var(--r-lg);
    }
  `,
})
export class ItemEditPage implements OnInit {
  private readonly api = inject(ItemService);
  private readonly route = inject(ActivatedRoute);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly kind = signal<'lost' | 'found'>('lost');
  readonly itemId = signal<number | null>(null);

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!Number.isFinite(id) || id <= 0) {
      this.loading.set(false);
      this.error.set('Bu ilan bulunamadı.');
      return;
    }

    this.itemId.set(id);
    this.api.get(id).subscribe({
      next: (item) => {
        this.kind.set(item.kind);
        this.loading.set(false);
      },
      error: (err: Error) => {
        this.error.set(err.message);
        this.loading.set(false);
      },
    });
  }
}
