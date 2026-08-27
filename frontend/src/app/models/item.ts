export type ItemKind = 'lost' | 'found';
export type ItemStatus = 'open' | 'claimed' | 'closed';
export type BoardFlow = 'all' | 'lost' | 'found';

export interface StatusHistoryEntry {
  status: ItemStatus;
  at: string | null;
}

export interface Item {
  id: number;
  title: string;
  description: string;
  location: string;
  category: string;
  contact: string | null;
  photoUrl: string | null;
  kind: ItemKind;
  status: ItemStatus;
  createdAt: string;
  statusHistory: StatusHistoryEntry[];
}

export interface ItemPayload {
  title: string;
  description: string;
  location: string;
  category: string;
  contact: string;
  photoUrl: string;
  kind: ItemKind;
  status: ItemStatus;
}

export interface ItemQuery {
  q?: string;
  category?: string;
  location?: string;
  status?: string;
  kind?: string;
}

export const KIND_LABELS: Record<ItemKind, string> = {
  lost: 'Kaybettim',
  found: 'Buldum',
};

export const STATUS_LABELS: Record<ItemStatus, string> = {
  open: 'açık',
  claimed: 'sahiplenildi',
  closed: 'kapandı',
};

export const STATUS_HISTORY_LABELS: Record<ItemStatus, string> = {
  open: 'Açıldı',
  claimed: 'Sahiplenildi',
  closed: 'Kapandı',
};

export const KIND_OPTIONS: ItemKind[] = ['lost', 'found'];
export const STATUS_OPTIONS: ItemStatus[] = ['open', 'claimed', 'closed'];

export const FALLBACK_LOCATIONS = [
  'merkez',
  'kütüphane',
  'yemekhane',
  'mühendislik',
  'yurt',
  'spor salonu',
] as const;

export const FALLBACK_CATEGORIES = [
  'öğrenci kartı',
  'anahtar',
  'telefon',
  'çanta',
  'kıyafet',
  'kulaklık',
  'diğer',
] as const;

export function sameLookup(a: string | null | undefined, b: string | null | undefined): boolean {
  return (a ?? '').trim().toLocaleLowerCase('tr-TR') === (b ?? '').trim().toLocaleLowerCase('tr-TR');
}

export function mergeLookups(apiValues: string[], fallback: readonly string[]): string[] {
  const seen = new Set<string>();
  const result: string[] = [];

  for (const value of [...fallback, ...apiValues]) {
    const normalized = value.trim();
    if (!normalized) {
      continue;
    }
    const key = normalized.toLocaleLowerCase('tr-TR');
    if (seen.has(key)) {
      continue;
    }
    seen.add(key);
    result.push(normalized);
  }

  return result;
}

export function ensureCurrentLookup(options: string[], current?: string | null): string[] {
  const value = current?.trim();
  if (!value) {
    return options;
  }
  if (options.some((option) => sameLookup(option, value))) {
    return options;
  }
  return [value, ...options];
}

export function canShowContact(item: Pick<Item, 'status' | 'contact'>): boolean {
  if (item.status === 'open') {
    return false;
  }
  return Boolean(item.contact?.trim());
}
