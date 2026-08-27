import { Component, input } from '@angular/core';
import { ItemKind, ItemStatus, KIND_LABELS, STATUS_LABELS } from '../models/item';

@Component({
  selector: 'app-kind-badge',
  template: `<span class="chip" [class]="'chip-' + kind()">{{ labels[kind()] }}</span>`,
})
export class KindBadge {
  readonly kind = input.required<ItemKind>();
  readonly labels = KIND_LABELS;
}

@Component({
  selector: 'app-status-badge',
  template: `<span class="chip" [class]="'chip-status-' + status()">{{ labels[status()] }}</span>`,
})
export class StatusBadge {
  readonly status = input.required<ItemStatus>();
  readonly labels = STATUS_LABELS;
}
