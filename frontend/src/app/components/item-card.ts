import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Item, KIND_LABELS } from '../models/item';
import { formatDateTr } from '../utils/format';
import { KindBadge, StatusBadge } from './badges';

@Component({
  selector: 'app-item-card',
  imports: [RouterLink, KindBadge, StatusBadge],
  templateUrl: './item-card.html',
  styleUrl: './item-card.css',
})
export class ItemCard {
  readonly item = input.required<Item>();
  readonly kindLabels = KIND_LABELS;
  readonly formatDate = formatDateTr;

  placeholder(kind: Item['kind']): string {
    return kind === 'lost' ? 'Kaybettim — görsel yok' : 'Buldum — görsel yok';
  }
}
