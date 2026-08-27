import { Injectable } from '@angular/core';

export const MANAGE_TOKEN_HEADER = 'X-Manage-Token';
export const CANNOT_MANAGE_ITEM_MESSAGE = 'Bu ilanı yönetemezsiniz.';

const STORAGE_KEY = 'kampus-kayip-esya.manageTokens';

@Injectable({ providedIn: 'root' })
export class ManageTokenStore {
  private readonly memory = new Map<string, string>();

  get(itemId: number): string | null {
    if (!isItemId(itemId)) {
      return null;
    }

    const key = String(itemId);
    const cached = this.memory.get(key);
    if (cached) {
      return cached;
    }

    const stored = this.read()[key];
    if (!stored) {
      return null;
    }

    this.memory.set(key, stored);
    return stored;
  }

  save(itemId: number, token: string): void {
    const value = token.trim();
    if (!isItemId(itemId) || !value) {
      return;
    }

    const key = String(itemId);
    this.memory.set(key, value);

    const map = this.read();
    map[key] = value;
    this.write(map);
  }

  remove(itemId: number): void {
    if (!isItemId(itemId)) {
      return;
    }

    const key = String(itemId);
    this.memory.delete(key);

    const map = this.read();
    if (!(key in map)) {
      return;
    }

    delete map[key];
    this.write(map);
  }

  private read(): Record<string, string> {
    try {
      const raw = globalThis.localStorage?.getItem(STORAGE_KEY);
      if (!raw) {
        return {};
      }

      const parsed: unknown = JSON.parse(raw);
      if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) {
        return {};
      }

      const result: Record<string, string> = {};
      for (const [key, value] of Object.entries(parsed)) {
        if (typeof value === 'string' && value.trim()) {
          result[key] = value.trim();
        }
      }
      return result;
    } catch {
      return {};
    }
  }

  private write(map: Record<string, string>): void {
    try {
      globalThis.localStorage?.setItem(STORAGE_KEY, JSON.stringify(map));
    } catch {
      // Private mode / quota: in-memory map still covers this session.
    }
  }
}

function isItemId(itemId: number): boolean {
  return Number.isInteger(itemId) && itemId > 0;
}
