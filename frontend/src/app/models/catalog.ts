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

export type CampusLocation = (typeof FALLBACK_LOCATIONS)[number];
export type CampusCategory = (typeof FALLBACK_CATEGORIES)[number];

export function mergeCatalog(preferred: readonly string[], extra: readonly string[] = []): string[] {
  const seen = new Set<string>();
  const result: string[] = [];

  for (const value of [...preferred, ...extra]) {
    const trimmed = value.trim();
    if (!trimmed) {
      continue;
    }
    const key = trimmed.toLocaleLowerCase('tr-TR');
    if (seen.has(key)) {
      continue;
    }
    seen.add(key);
    result.push(trimmed);
  }

  return result;
}
