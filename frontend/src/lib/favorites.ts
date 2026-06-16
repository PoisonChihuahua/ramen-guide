const STORAGE_KEY = 'ramensite_favorites';

/**
 * localStorage からお気に入り店舗 ID の配列を読み出す。
 * 不正なデータが入っていても空配列にフォールバックする。
 */
export function readFavorites(): number[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return [];
    const parsed: unknown = JSON.parse(raw);
    if (!Array.isArray(parsed)) return [];
    return parsed.filter((n): n is number => typeof n === 'number');
  } catch {
    return [];
  }
}

/** お気に入り店舗 ID の配列を localStorage に書き込む。 */
export function writeFavorites(ids: number[]): void {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(ids));
}
