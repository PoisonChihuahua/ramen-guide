import { apiFetch } from './client';
import type { Review, ReviewInput } from '../types';

/** 店舗のレビュー一覧（新しい順・公開）。 */
export function fetchReviews(shopId: number): Promise<Review[]> {
  return apiFetch<Review[]>(`/api/shops/${shopId}/reviews`);
}

/** レビューを投稿または更新（要認証）。 */
export function submitReview(
  shopId: number,
  input: ReviewInput,
): Promise<Review> {
  return apiFetch<Review>(`/api/shops/${shopId}/reviews`, {
    method: 'POST',
    body: input,
  });
}

/** 自分のレビューを削除（要認証）。 */
export function deleteReview(shopId: number): Promise<void> {
  return apiFetch<void>(`/api/shops/${shopId}/reviews`, {
    method: 'DELETE',
  });
}
