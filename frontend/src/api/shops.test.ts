import { describe, it, expect, vi, beforeEach } from 'vitest';
import { fetchShops, fetchShop, createShop, updateShop, deleteShop } from './shops';
import { apiFetch } from './client';
import type { Shop, ShopInput } from '../types';

vi.mock('./client', () => ({ apiFetch: vi.fn() }));

const mockedApiFetch = vi.mocked(apiFetch);

const shop: Shop = {
  id: 1,
  name: '麺屋テスト',
  description: '説明',
  address: '東京都1-1-1',
  area: '東京',
  genre: '醤油',
  openingHours: '11:00〜22:00',
  priceRange: '¥800〜',
  imageUrl: 'https://example.com/x.jpg',
  averageRating: 4.2,
  reviewCount: 5,
};

const input: ShopInput = {
  name: '新店',
  description: '説明',
  address: '東京都1-1-1',
  area: '東京',
  genre: '塩',
  openingHours: '10:00〜21:00',
  priceRange: '¥700〜',
  imageUrl: 'https://example.com/y.jpg',
};

describe('shops API', () => {
  beforeEach(() => {
    mockedApiFetch.mockReset();
  });

  describe('fetchShops', () => {
    it('フィルタなしで /api/shops を呼ぶ', async () => {
      mockedApiFetch.mockResolvedValue([shop]);
      await fetchShops();
      expect(mockedApiFetch).toHaveBeenCalledWith('/api/shops');
    });

    it('キーワードフィルタ付きでクエリパラメータを送る', async () => {
      mockedApiFetch.mockResolvedValue([shop]);
      await fetchShops({ q: 'ラーメン' });
      expect(mockedApiFetch).toHaveBeenCalledWith('/api/shops?q=%E3%83%A9%E3%83%BC%E3%83%A1%E3%83%B3');
    });

    it('ジャンル・エリア複合フィルタを送る', async () => {
      mockedApiFetch.mockResolvedValue([shop]);
      await fetchShops({ genre: '醤油', area: '東京' });
      const url = (mockedApiFetch.mock.calls[0][0] as string);
      expect(url).toContain('genre=');
      expect(url).toContain('area=');
    });
  });

  it('fetchShop: GET /api/shops/:id を呼ぶ', async () => {
    mockedApiFetch.mockResolvedValue(shop);
    const result = await fetchShop(1);
    expect(mockedApiFetch).toHaveBeenCalledWith('/api/shops/1');
    expect(result).toEqual(shop);
  });

  it('createShop: POST /api/shops に ShopInput を送る', async () => {
    mockedApiFetch.mockResolvedValue({ ...shop, ...input });
    await createShop(input);
    expect(mockedApiFetch).toHaveBeenCalledWith('/api/shops', { method: 'POST', body: input });
  });

  it('updateShop: PUT /api/shops/:id に ShopInput を送る', async () => {
    mockedApiFetch.mockResolvedValue({ ...shop, ...input });
    await updateShop(1, input);
    expect(mockedApiFetch).toHaveBeenCalledWith('/api/shops/1', { method: 'PUT', body: input });
  });

  it('deleteShop: DELETE /api/shops/:id を呼ぶ', async () => {
    mockedApiFetch.mockResolvedValue(undefined);
    await deleteShop(1);
    expect(mockedApiFetch).toHaveBeenCalledWith('/api/shops/1', { method: 'DELETE' });
  });
});
