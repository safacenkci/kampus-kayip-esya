import { Routes } from '@angular/router';
import { FlowChooser } from './pages/flow-chooser';
import { ItemDetail } from './pages/item-detail';
import { ItemForm } from './pages/item-form';
import { ItemList } from './pages/item-list';

export const routes: Routes = [
  { path: '', component: ItemList, data: { flow: 'all' } },
  { path: 'kaybettim', component: ItemList, data: { flow: 'lost' } },
  { path: 'kaybettim/yeni', component: ItemForm, data: { kind: 'lost' } },
  { path: 'buldum', component: ItemList, data: { flow: 'found' } },
  { path: 'buldum/yeni', component: ItemForm, data: { kind: 'found' } },
  { path: 'yeni', component: FlowChooser },
  { path: 'ilan/:id/duzenle', component: ItemForm },
  { path: 'ilan/:id', component: ItemDetail },
  { path: '**', redirectTo: '' },
];
