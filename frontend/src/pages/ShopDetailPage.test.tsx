import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ShopDetailPage } from './ShopDetailPage';
import { fetchShop } from '../api/shops';
import type { Shop } from '../types';

vi.mock('../api/shops', () => ({ fetchShop: vi.fn() }));
vi.mock('../components/FavoriteButton', () => ({
  FavoriteButton: () => <button type="button">お気に入り</button>,
}));
vi.mock('../components/ReviewSection', () => ({
  ReviewSection: () => <div>レビューセクション</div>,
}));
vi.mock('../components/StarRating', () => ({
  StarRating: () => <div>★</div>,
}));

const mockedFetchShop = vi.mocked(fetchShop);

const shop: Shop = {
  id: 1,
  name: '麺屋 テスト',
  description: 'テスト説明',
  address: '東京都1-1-1',
  area: '東京',
  genre: '醤油',
  openingHours: '11:00〜22:00',
  priceRange: '¥800〜',
  imageUrl: 'https://example.com/x.jpg',
  averageRating: 4.5,
  reviewCount: 10,
};

function renderPage(path = '/shops/1') {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          <Route path="/shops/:id" element={<ShopDetailPage />} />
          <Route path="/" element={<div>ホーム</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ShopDetailPage', () => {
  beforeEach(() => {
    mockedFetchShop.mockReset();
  });

  it('取得中はローディングを表示する', () => {
    mockedFetchShop.mockReturnValue(new Promise<Shop>(() => {}));
    renderPage();
    expect(screen.getByText('読み込み中...')).toBeInTheDocument();
  });

  it('取得エラー時はエラーメッセージを表示する', async () => {
    mockedFetchShop.mockRejectedValue(new Error('not found'));
    renderPage();
    expect(await screen.findByText('店舗が見つかりませんでした。')).toBeInTheDocument();
  });

  it('店舗情報を描画する', async () => {
    mockedFetchShop.mockResolvedValue(shop);
    renderPage();
    expect(await screen.findByRole('heading', { name: '麺屋 テスト' })).toBeInTheDocument();
    expect(screen.getByText('テスト説明')).toBeInTheDocument();
    expect(screen.getByText('東京都1-1-1')).toBeInTheDocument();
    expect(screen.getByText('11:00〜22:00')).toBeInTheDocument();
  });

  it('レビューがある場合は StarRating を描画する', async () => {
    mockedFetchShop.mockResolvedValue(shop);
    renderPage();
    await screen.findByRole('heading', { name: '麺屋 テスト' });
    expect(screen.getByText('★')).toBeInTheDocument();
  });

  it('レビューがない場合は StarRating を描画しない', async () => {
    mockedFetchShop.mockResolvedValue({ ...shop, reviewCount: 0 });
    renderPage();
    await screen.findByRole('heading', { name: '麺屋 テスト' });
    expect(screen.queryByText('★')).not.toBeInTheDocument();
  });
});
