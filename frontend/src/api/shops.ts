import { apiFetch } from './client';
import type { Shop, ShopFilters } from '../types';

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
