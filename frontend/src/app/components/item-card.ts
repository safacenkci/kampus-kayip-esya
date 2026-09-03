import { ChangeDetectionStrategy, Component, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Item, KIND_LABELS } from '../models/item';
import { formatRelativeTr } from '../utils/format';
import { KindBadge, StatusBadge } from './badges';
import { CategoryIcon } from './category-icon';

@Component({
  selector: 'app-item-card',
  imports: [RouterLink, KindBadge, StatusBadge, CategoryIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './item-card.html',
  styleUrl: './item-card.css',
})
export class ItemCard {
  readonly item = input.required<Item>();
  readonly kindLabels = KIND_LABELS;
  readonly relative = formatRelativeTr;
  readonly photoBroken = signal(false);

  onPhotoError(): void {
    this.photoBroken.set(true);
  }
}
