export type ItemKind = 'lost' | 'found';
export type ItemStatus = 'open' | 'claimed' | 'closed';

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
