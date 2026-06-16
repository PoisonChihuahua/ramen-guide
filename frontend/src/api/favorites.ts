import { apiFetch } from './client';
import type { FavoriteStatus, Shop } from '../types';

// 認証は httpOnly Cookie（apiFetch が credentials: 'include' で送信）で行う。

/** お気に入り登録した店舗一覧（要認証）。 */
export function fetchFavorites(): Promise<Shop[]> {
  return apiFetch<Shop[]>('/api/favorites');
}

/** 指定店舗のお気に入り状態を取得（要認証）。 */
export function fetchFavoriteStatus(shopId: number): Promise<FavoriteStatus> {
  return apiFetch<FavoriteStatus>(`/api/favorites/${shopId}/status`);
}

/** 店舗をお気に入りに追加（要認証・冪等）。 */
export function addFavorite(shopId: number): Promise<FavoriteStatus> {
  return apiFetch<FavoriteStatus>(`/api/favorites/${shopId}`, {
    method: 'PUT',
  });
}

/** お気に入りから外す（要認証・冪等）。 */
export function removeFavorite(shopId: number): Promise<FavoriteStatus> {
  return apiFetch<FavoriteStatus>(`/api/favorites/${shopId}`, {
    method: 'DELETE',
  });
}
