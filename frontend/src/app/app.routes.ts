import { CanMatchFn, Routes } from '@angular/router';
import { ItemDetail } from './pages/item-detail';
import { ItemForm } from './pages/item-form';
import { ItemList } from './pages/item-list';

const isNumericId: CanMatchFn = (_route, segments) => /^\d+$/.test(segments[0]?.path ?? '');

export const routes: Routes = [
  { path: '', component: ItemList },
  { path: 'yeni', component: ItemForm },
  { path: ':id/duzenle', canMatch: [isNumericId], component: ItemForm },
  { path: ':id', canMatch: [isNumericId], component: ItemDetail },
  { path: '**', redirectTo: '' },
];
