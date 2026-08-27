export type ItemKind = 'lost' | 'found';
export type ItemStatus = 'open' | 'claimed' | 'closed';

export interface StatusEvent {
  status: ItemStatus;
  at: string;
  note?: string;
}

export interface Item {
  id: number;
  title: string;
  description: string;
  location: string;
  category: string;
  contact: string;
  photoUrl: string | null;
  kind: ItemKind;
  status: ItemStatus;
  createdAt: string;
  statusHistory: StatusEvent[];
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
  lost: 'Kayıp',
  found: 'Bulunan',
};

export const STATUS_LABELS: Record<ItemStatus, string> = {
  open: 'Açık',
  claimed: 'Sahiplenildi',
  closed: 'Kapandı',
};

export const KIND_OPTIONS: ItemKind[] = ['lost', 'found'];
export const STATUS_OPTIONS: ItemStatus[] = ['open', 'claimed', 'closed'];

export function isContactVisible(status: ItemStatus): boolean {
  return status === 'claimed' || status === 'closed';
}

export function oppositeKind(kind: ItemKind): ItemKind {
  return kind === 'lost' ? 'found' : 'lost';
}

export function sameCampusField(a: string, b: string): boolean {
  return a.trim().toLocaleLowerCase('tr-TR') === b.trim().toLocaleLowerCase('tr-TR');
}
