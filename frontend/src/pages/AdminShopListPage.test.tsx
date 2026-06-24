import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AdminShopListPage } from './AdminShopListPage';
import { fetchShops, deleteShop } from '../api/shops';
import type { Shop, PagedResult } from '../types';

vi.mock('../api/shops', () => ({
  fetchShops: vi.fn(),
  deleteShop: vi.fn(),
}));

const mockedFetchShops = vi.mocked(fetchShops);
const mockedDeleteShop = vi.mocked(deleteShop);

const shopList: Shop[] = [
  {
    id: 1,
    name: '麺屋 一',
    description: '説明',
    address: '東京都',
    area: '東京',
    genre: '醤油',
    openingHours: '11:00〜',
    priceRange: '¥800〜',
    imageUrl: '',
    averageRating: 4.0,
    reviewCount: 3,
  },
  {
    id: 2,
    name: '麺屋 二',
    description: '説明',
    address: '大阪府',
    area: '大阪',
    genre: '味噌',
    openingHours: '10:00〜',
    priceRange: '¥700〜',
    imageUrl: '',
    averageRating: 0,
    reviewCount: 0,
  },
];

function makePagedResult(shops: Shop[]): PagedResult<Shop> {
  return { items: shops, total: shops.length, page: 1, limit: 20 };
}

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <AdminShopListPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('AdminShopListPage', () => {
  beforeEach(() => {
    mockedFetchShops.mockReset();
    mockedDeleteShop.mockReset();
  });

  it('取得中はローディングを表示する', () => {
    mockedFetchShops.mockReturnValue(new Promise<PagedResult<Shop>>(() => {}));
    renderPage();
    expect(screen.getByText('読み込み中...')).toBeInTheDocument();
  });

  it('取得エラー時はエラーメッセージを表示する', async () => {
    mockedFetchShops.mockRejectedValue(new Error('error'));
    renderPage();
    expect(await screen.findByText('店舗情報の取得に失敗しました。')).toBeInTheDocument();
  });

  it('店舗一覧をテーブルで描画する', async () => {
    mockedFetchShops.mockResolvedValue(makePagedResult(shopList));
    renderPage();
    expect(await screen.findByText('麺屋 一')).toBeInTheDocument();
    expect(screen.getByText('麺屋 二')).toBeInTheDocument();
    expect(screen.getByText('★4.0 (3)')).toBeInTheDocument();
    expect(screen.getAllByText('—').length).toBeGreaterThan(0);
  });

  it('削除確認でOKを押すと deleteShop が呼ばれる', async () => {
    const u = userEvent.setup();
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    mockedFetchShops.mockResolvedValue(makePagedResult(shopList));
    mockedDeleteShop.mockResolvedValue(undefined);
    renderPage();

    const deleteButtons = await screen.findAllByRole('button', { name: '削除' });
    await u.click(deleteButtons[0]);

    expect(mockedDeleteShop).toHaveBeenCalledWith(1);
  });

  it('削除確認でキャンセルを押すと deleteShop を呼ばない', async () => {
    const u = userEvent.setup();
    vi.spyOn(window, 'confirm').mockReturnValue(false);
    mockedFetchShops.mockResolvedValue(makePagedResult(shopList));
    renderPage();

    const deleteButtons = await screen.findAllByRole('button', { name: '削除' });
    await u.click(deleteButtons[0]);

    expect(mockedDeleteShop).not.toHaveBeenCalled();
  });
});
