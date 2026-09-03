import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { ThemeService } from '../services/theme.service';

@Component({
  selector: 'app-site-header',
  imports: [RouterLink, RouterLinkActive],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './site-header.html',
  styleUrl: './site-header.css',
})
export class SiteHeader {
  private readonly theme = inject(ThemeService);
  readonly choice = this.theme.choice;

  toggleTheme(): void {
    this.theme.cycle();
  }

  themeLabel(): string {
    return this.theme.label();
  }
}
