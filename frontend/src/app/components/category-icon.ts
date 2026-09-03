import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

type IconKey = 'kart' | 'anahtar' | 'telefon' | 'canta' | 'kiyafet' | 'kulaklik' | 'diger';

/** Kategori adını, Türkçe küçültme ve aksan sadeleştirmesiyle bir ikona eşler. */
export function iconKeyFor(category: string | null | undefined): IconKey {
  const normalized = (category ?? '')
    .toLocaleLowerCase('tr-TR')
    .replace(/ı/g, 'i')
    .replace(/ğ/g, 'g')
    .replace(/ü/g, 'u')
    .replace(/ş/g, 's')
    .replace(/ö/g, 'o')
    .replace(/ç/g, 'c')
    .trim();

  if (normalized.includes('kart') || normalized.includes('kimlik')) return 'kart';
  if (normalized.includes('anahtar')) return 'anahtar';
  if (normalized.includes('telefon')) return 'telefon';
  if (normalized.includes('canta') || normalized.includes('sirt')) return 'canta';
  if (normalized.includes('kiyafet') || normalized.includes('mont')) return 'kiyafet';
  if (normalized.includes('kulaklik')) return 'kulaklik';
  return 'diger';
}

@Component({
  selector: 'app-category-icon',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      stroke-width="1.7"
      stroke-linecap="round"
      stroke-linejoin="round"
      aria-hidden="true"
      focusable="false"
    >
      @switch (key()) {
        @case ('kart') {
          <rect x="2.5" y="5" width="19" height="14" rx="2.5" />
          <path d="M2.5 9.5h19" />
          <path d="M6 14h4" />
        }
        @case ('anahtar') {
          <circle cx="8" cy="8" r="4" />
          <path d="M10.9 10.9 20 20" />
          <path d="M17 17l2-2" />
          <path d="M14.5 14.5l2.5-2.5" />
        }
        @case ('telefon') {
          <rect x="6" y="2.5" width="12" height="19" rx="2.6" />
          <path d="M10.5 18.5h3" />
        }
        @case ('canta') {
          <path d="M4.5 8.5h15l1 12h-17z" />
          <path d="M8.5 8.5V6a3.5 3.5 0 0 1 7 0v2.5" />
        }
        @case ('kiyafet') {
          <path d="M9 3.5 12 6l3-2.5 5.5 3.2-2.2 4-2.3-1v7.8H8v-7.8l-2.3 1-2.2-4z" />
        }
        @case ('kulaklik') {
          <path d="M4 14v-2a8 8 0 0 1 16 0v2" />
          <rect x="2.5" y="13.5" width="4.5" height="7" rx="2.2" />
          <rect x="17" y="13.5" width="4.5" height="7" rx="2.2" />
        }
        @default {
          <circle cx="12" cy="12" r="9" />
          <path d="M9.5 9.6a2.6 2.6 0 1 1 3.4 2.5c-.6.2-.9.7-.9 1.3v.4" />
          <path d="M12 17.2h.01" />
        }
      }
    </svg>
  `,
  styles: `
    :host {
      display: inline-grid;
      place-items: center;
      width: 1.15em;
      height: 1.15em;
      flex: none;
    }

    svg {
      width: 100%;
      height: 100%;
    }
  `,
})
export class CategoryIcon {
  readonly category = input.required<string | null | undefined>();
  readonly key = computed(() => iconKeyFor(this.category()));
}
