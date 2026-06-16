import { describe, it, expect, vi, beforeEach } from 'vitest';
import { fetchReviews, submitReview, deleteReview } from './reviews';
import { apiFetch } from './client';
import type { Review } from '../types';

vi.mock('./client', () => ({ apiFetch: vi.fn() }));

const mockedApiFetch = vi.mocked(apiFetch);

const review: Review = {
  id: 1,
  shopId: 1,
  userId: 2,
  displayName: '太郎',
  rating: 5,
  comment: '最高でした',
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: '2026-01-01T00:00:00Z',
};

describe('reviews API', () => {
  beforeEach(() => {
    mockedApiFetch.mockReset();
  });

  it('fetchReviews: GET /api/shops/:shopId/reviews を呼ぶ', async () => {
    mockedApiFetch.mockResolvedValue([review]);
    const result = await fetchReviews(1);
    expect(mockedApiFetch).toHaveBeenCalledWith('/api/shops/1/reviews');
    expect(result).toEqual([review]);
  });

  it('submitReview: POST /api/shops/:shopId/reviews にレビューを送る', async () => {
    mockedApiFetch.mockResolvedValue(review);
    const result = await submitReview(1, { rating: 5, comment: '最高でした' });
    expect(mockedApiFetch).toHaveBeenCalledWith('/api/shops/1/reviews', {
      method: 'POST',
      body: { rating: 5, comment: '最高でした' },
    });
    expect(result).toEqual(review);
  });

  it('deleteReview: DELETE /api/shops/:shopId/reviews を呼ぶ', async () => {
    mockedApiFetch.mockResolvedValue(undefined);
    await deleteReview(1);
    expect(mockedApiFetch).toHaveBeenCalledWith('/api/shops/1/reviews', { method: 'DELETE' });
  });
});
