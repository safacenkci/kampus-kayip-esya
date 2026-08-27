import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LOST_FLOW } from '../models/flows';
import { NoticeWizard } from './notice-wizard';

@Component({
  selector: 'app-kaybettim',
  imports: [RouterLink, NoticeWizard],
  templateUrl: './kaybettim.html',
  styleUrl: './kaybettim.css',
})
export class KaybettimPage {
  readonly editId = input<number | null>(null);
  readonly copy = LOST_FLOW;
}
