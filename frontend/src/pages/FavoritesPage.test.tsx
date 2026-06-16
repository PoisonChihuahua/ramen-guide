import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { FavoritesPage } from './FavoritesPage';
import { fetchFavorites } from '../api/favorites';
import type { Shop } from '../types';

vi.mock('../api/favorites', () => ({ fetchFavorites: vi.fn() }));

const mockedFetchFavorites = vi.mocked(fetchFavorites);

function makeShop(id: number, name: string): Shop {
  return {
    id,
    name,
    description: `${name}の説明`,
    address: '東京都1-1-1',
    area: '東京',
    genre: '醤油',
    openingHours: '11:00〜22:00',
    priceRange: '¥800〜',
    imageUrl: '',
    averageRating: 0,
    reviewCount: 0,
  };
}

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <FavoritesPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('FavoritesPage', () => {
  beforeEach(() => {
    mockedFetchFavorites.mockReset();
  });

  it('取得中はローディングを表示する', () => {
    mockedFetchFavorites.mockReturnValue(new Promise<Shop[]>(() => {}));
    renderPage();
    expect(screen.getByText('読み込み中...')).toBeInTheDocument();
  });

  it('取得エラー時はエラーメッセージを表示する', async () => {
    mockedFetchFavorites.mockRejectedValue(new Error('network error'));
    renderPage();
    expect(await screen.findByText('お気に入りの取得に失敗しました。')).toBeInTheDocument();
  });

  it('お気に入りが空のとき空メッセージを表示する', async () => {
    mockedFetchFavorites.mockResolvedValue([]);
    renderPage();
    expect(await screen.findByText(/まだお気に入りがありません/)).toBeInTheDocument();
  });

  it('お気に入り店舗を一覧表示する', async () => {
    mockedFetchFavorites.mockResolvedValue([
      makeShop(1, '麺屋 あ'),
      makeShop(2, '麺屋 い'),
    ]);
    renderPage();
    expect(await screen.findByText('麺屋 あ')).toBeInTheDocument();
    expect(screen.getByText('麺屋 い')).toBeInTheDocument();
    expect(screen.getByText('全 2 店舗')).toBeInTheDocument();
  });
});
