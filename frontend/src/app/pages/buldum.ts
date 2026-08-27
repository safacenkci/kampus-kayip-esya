import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FOUND_FLOW } from '../models/flows';
import { NoticeWizard } from './notice-wizard';

@Component({
  selector: 'app-buldum',
  imports: [RouterLink, NoticeWizard],
  templateUrl: './buldum.html',
  styleUrl: './buldum.css',
})
export class BuldumPage {
  readonly editId = input<number | null>(null);
  readonly copy = FOUND_FLOW;
}
