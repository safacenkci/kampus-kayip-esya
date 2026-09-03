export function formatDateTr(value: string | null | undefined): string {
  const date = toDate(value);
  if (!date) {
    return value ? String(value) : '—';
  }

  return date.toLocaleString('tr-TR', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

/** Kart ve liste için kısa tarih: "27 Ağu 09:05". */
export function formatShortTr(value: string | null | undefined): string {
  const date = toDate(value);
  if (!date) {
    return value ? String(value) : '—';
  }

  return date.toLocaleString('tr-TR', {
    day: 'numeric',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
  });
}

/**
 * Göreli zaman: "az önce", "3 saat önce", "dün", "5 gün önce".
 * Bir haftadan eskiyse kısa tarihe düşer.
 */
export function formatRelativeTr(value: string | null | undefined, now = new Date()): string {
  const date = toDate(value);
  if (!date) {
    return '—';
  }

  const diffMs = now.getTime() - date.getTime();
  const minutes = Math.round(diffMs / 60000);

  if (minutes < 0) {
    return formatShortTr(value);
  }
  if (minutes < 2) {
    return 'az önce';
  }
  if (minutes < 60) {
    return `${minutes} dakika önce`;
  }

  const hours = Math.floor(minutes / 60);
  if (hours < 24) {
    return `${hours} saat önce`;
  }

  const days = Math.floor(hours / 24);
  if (days === 1) {
    return 'dün';
  }
  if (days < 7) {
    return `${days} gün önce`;
  }

  return formatShortTr(value);
}

function toDate(value: string | null | undefined): Date | null {
  if (!value) {
    return null;
  }
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? null : date;
}
