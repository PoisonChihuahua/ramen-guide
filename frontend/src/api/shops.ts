import { apiFetch } from './client';
import type { Shop, ShopFilters, ShopInput, PagedResult, ShopOptions } from '../types';

export function fetchShops(filters: ShopFilters = {}): Promise<PagedResult<Shop>> {
  const params = new URLSearchParams();
  if (filters.q) params.set('q', filters.q);
  if (filters.genre) params.set('genre', filters.genre);
  if (filters.area) params.set('area', filters.area);
  if (filters.page) params.set('page', String(filters.page));
  if (filters.limit) params.set('limit', String(filters.limit));

  const query = params.toString();
  return apiFetch<PagedResult<Shop>>(`/api/shops${query ? `?${query}` : ''}`);
}

export function fetchShopOptions(): Promise<ShopOptions> {
  return apiFetch<ShopOptions>('/api/shops/options');
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
