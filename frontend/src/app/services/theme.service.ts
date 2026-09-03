import { Injectable, signal } from '@angular/core';

export type ThemeChoice = 'light' | 'dark' | 'system';

const STORAGE_KEY = 'kke-theme';

/**
 * Tema tercihini yönetir. Üç durum vardır: açık, koyu ve sistem.
 * "Sistem" seçiliyken kök öğede işaret bırakılmaz; prefers-color-scheme geçerli olur.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly choice = signal<ThemeChoice>(this.read());

  constructor() {
    this.apply(this.choice());
  }

  /** Açık → koyu → sistem sırasıyla döner. */
  cycle(): void {
    const order: ThemeChoice[] = ['light', 'dark', 'system'];
    const next = order[(order.indexOf(this.choice()) + 1) % order.length];
    this.set(next);
  }

  set(choice: ThemeChoice): void {
    this.choice.set(choice);
    this.apply(choice);
    this.write(choice);
  }

  /** Şu an ekranda görünen tema — sistem seçiliyse işletim sistemine bakar. */
  resolved(): 'light' | 'dark' {
    const choice = this.choice();
    if (choice !== 'system') {
      return choice;
    }
    return this.prefersDark() ? 'dark' : 'light';
  }

  label(): string {
    switch (this.choice()) {
      case 'light':
        return 'Açık tema';
      case 'dark':
        return 'Koyu tema';
      default:
        return 'Sistem teması';
    }
  }

  private apply(choice: ThemeChoice): void {
    const root = document?.documentElement;
    if (!root) {
      return;
    }
    if (choice === 'system') {
      root.removeAttribute('data-theme');
    } else {
      root.setAttribute('data-theme', choice);
    }
  }

  private prefersDark(): boolean {
    try {
      return window.matchMedia('(prefers-color-scheme: dark)').matches;
    } catch {
      return false;
    }
  }

  private read(): ThemeChoice {
    try {
      const saved = localStorage.getItem(STORAGE_KEY);
      if (saved === 'light' || saved === 'dark' || saved === 'system') {
        return saved;
      }
    } catch {
      /* depolama kapalı olabilir */
    }
    return 'system';
  }

  private write(choice: ThemeChoice): void {
    try {
      if (choice === 'system') {
        localStorage.removeItem(STORAGE_KEY);
      } else {
        localStorage.setItem(STORAGE_KEY, choice);
      }
    } catch {
      /* depolama kapalıysa tercih yalnız bu sekmede geçerli olur */
    }
  }
}
