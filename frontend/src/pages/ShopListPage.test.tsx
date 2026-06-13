import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ShopListPage } from './ShopListPage';
import { fetchShops } from '../api/shops';
import type { Shop } from '../types';

// API レイヤをモックし、ページの描画振る舞いだけを検証する
vi.mock('../api/shops', () => ({
  fetchShops: vi.fn(),
}));

const mockedFetchShops = vi.mocked(fetchShops);

function makeShop(id: number, name: string): Shop {
  return {
    id,
    name,
    description: `${name} の説明`,
    address: '東京都1-1-1',
    area: '東京',
    genre: '醤油',
    openingHours: '11:00〜22:00',
    priceRange: '¥800〜¥1,000',
    imageUrl: 'https://example.com/x.jpg',
  };
}

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ShopListPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ShopListPage', () => {
  beforeEach(() => {
    mockedFetchShops.mockReset();
  });

  it('取得した店舗をカードとして描画する', async () => {
    mockedFetchShops.mockResolvedValue([
      makeShop(1, '麺屋 いちばん'),
      makeShop(2, '麺屋 にばん'),
    ]);

    renderPage();

    expect(await screen.findByText('麺屋 いちばん')).toBeInTheDocument();
    expect(screen.getByText('麺屋 にばん')).toBeInTheDocument();
    expect(screen.getByText('全 2 店舗')).toBeInTheDocument();
  });

  it('該当0件のとき空メッセージを表示する', async () => {
    mockedFetchShops.mockResolvedValue([]);

    renderPage();

    expect(
      await screen.findByText('条件に一致する店舗が見つかりませんでした。'),
    ).toBeInTheDocument();
  });

  it('取得中はローディングを表示する', () => {
    // 解決しない Promise でローディング状態を保持
    mockedFetchShops.mockReturnValue(new Promise<Shop[]>(() => {}));

    renderPage();

    expect(screen.getByText('読み込み中...')).toBeInTheDocument();
  });
});
