import { describe, it, expect, vi, beforeEach } from 'vitest';
import { fetchFavorites, fetchFavoriteStatus, addFavorite, removeFavorite } from './favorites';
import { apiFetch } from './client';
import type { FavoriteStatus, Shop } from '../types';

vi.mock('./client', () => ({ apiFetch: vi.fn() }));

const mockedApiFetch = vi.mocked(apiFetch);

const shops: Shop[] = [
  {
    id: 1,
    name: '麺屋',
    description: '説明',
    address: '東京',
    area: '東京',
    genre: '醤油',
    openingHours: '11:00〜',
    priceRange: '¥800〜',
    imageUrl: '',
    averageRating: 0,
    reviewCount: 0,
  },
];

const status: FavoriteStatus = { shopId: 1, isFavorite: true };

describe('favorites API', () => {
  beforeEach(() => {
    mockedApiFetch.mockReset();
  });

  it('fetchFavorites: GET /api/favorites を呼ぶ', async () => {
    mockedApiFetch.mockResolvedValue(shops);
    const result = await fetchFavorites();
    expect(mockedApiFetch).toHaveBeenCalledWith('/api/favorites');
    expect(result).toEqual(shops);
  });

  it('fetchFavoriteStatus: GET /api/favorites/:id/status を呼ぶ', async () => {
    mockedApiFetch.mockResolvedValue(status);
    const result = await fetchFavoriteStatus(1);
    expect(mockedApiFetch).toHaveBeenCalledWith('/api/favorites/1/status');
    expect(result).toEqual(status);
  });

  it('addFavorite: PUT /api/favorites/:id を呼ぶ', async () => {
    mockedApiFetch.mockResolvedValue(status);
    await addFavorite(1);
    expect(mockedApiFetch).toHaveBeenCalledWith('/api/favorites/1', { method: 'PUT' });
  });

  it('removeFavorite: DELETE /api/favorites/:id を呼ぶ', async () => {
    mockedApiFetch.mockResolvedValue({ shopId: 1, isFavorite: false });
    await removeFavorite(1);
    expect(mockedApiFetch).toHaveBeenCalledWith('/api/favorites/1', { method: 'DELETE' });
  });
});
