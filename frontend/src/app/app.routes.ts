import { CanMatchFn, Routes } from '@angular/router';
import { BuldumPage } from './pages/buldum';
import { ItemDetail } from './pages/item-detail';
import { ItemEditPage } from './pages/item-edit';
import { ItemList } from './pages/item-list';
import { KaybettimPage } from './pages/kaybettim';

const isNumericId: CanMatchFn = (_route, segments) => /^\d+$/.test(segments[0]?.path ?? '');

export const routes: Routes = [
  { path: '', component: ItemList },
  { path: 'kaybettim', component: KaybettimPage },
  { path: 'buldum', component: BuldumPage },
  { path: ':id/duzenle', canMatch: [isNumericId], component: ItemEditPage },
  { path: ':id', canMatch: [isNumericId], component: ItemDetail },
  { path: 'yeni', redirectTo: '' },
  { path: '**', redirectTo: '' },
];
