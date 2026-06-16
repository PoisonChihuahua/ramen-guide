import { apiFetch } from './client';
import type { Shop, ShopFilters, ShopInput } from '../types';

export function fetchShops(filters: ShopFilters = {}): Promise<Shop[]> {
  const params = new URLSearchParams();
  if (filters.q) params.set('q', filters.q);
  if (filters.genre) params.set('genre', filters.genre);
  if (filters.area) params.set('area', filters.area);

  const query = params.toString();
  return apiFetch<Shop[]>(`/api/shops${query ? `?${query}` : ''}`);
}

export function fetchShop(id: number): Promise<Shop> {
  return apiFetch<Shop>(`/api/shops/${id}`);
}

/** 店舗を新規登録（管理者のみ）。 */
export function createShop(input: ShopInput): Promise<Shop> {
  return apiFetch<Shop>('/api/shops', {
    method: 'POST',
    body: input,
  });
}

/** 店舗情報を更新（管理者のみ）。 */
export function updateShop(id: number, input: ShopInput): Promise<Shop> {
  return apiFetch<Shop>(`/api/shops/${id}`, {
    method: 'PUT',
    body: input,
  });
}

/** 店舗を削除（管理者のみ）。 */
export function deleteShop(id: number): Promise<void> {
  return apiFetch<void>(`/api/shops/${id}`, {
    method: 'DELETE',
  });
}
