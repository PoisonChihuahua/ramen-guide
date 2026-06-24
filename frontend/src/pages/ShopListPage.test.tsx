import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ShopListPage } from './ShopListPage';
import { fetchShops, fetchShopOptions } from '../api/shops';
import type { Shop, PagedResult } from '../types';

// API レイヤをモックし、ページの描画振る舞いだけを検証する
vi.mock('../api/shops', () => ({
  fetchShops: vi.fn(),
  fetchShopOptions: vi.fn(),
}));

const mockedFetchShops = vi.mocked(fetchShops);
const mockedFetchShopOptions = vi.mocked(fetchShopOptions);

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
    averageRating: 0,
    reviewCount: 0,
  };
}

function makePagedResult(shops: Shop[], page = 1, limit = 20): PagedResult<Shop> {
  return { items: shops, total: shops.length, page, limit };
}

type InitialEntry = string | { pathname: string; hash?: string; search?: string };

function renderPage(initialEntries?: InitialEntry[]) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={initialEntries}>
        <ShopListPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ShopListPage', () => {
  beforeEach(() => {
    mockedFetchShops.mockReset();
    mockedFetchShopOptions.mockReset();
    mockedFetchShopOptions.mockResolvedValue({ genres: ['醤油', '味噌'], areas: ['東京', '札幌'] });
  });

  it('取得した店舗をカードとして描画する', async () => {
    mockedFetchShops.mockResolvedValue(
      makePagedResult([makeShop(1, '麺屋 いちばん'), makeShop(2, '麺屋 にばん')]),
    );

    renderPage();

    expect(await screen.findByText('麺屋 いちばん')).toBeInTheDocument();
    expect(screen.getByText('麺屋 にばん')).toBeInTheDocument();
    expect(screen.getByText('全 2 店舗')).toBeInTheDocument();
  });

  it('該当0件のとき空メッセージを表示する', async () => {
    mockedFetchShops.mockResolvedValue(makePagedResult([]));

    renderPage();

    expect(
      await screen.findByText('条件に一致する店舗が見つかりませんでした。'),
    ).toBeInTheDocument();
  });

  it('取得中はローディングを表示する', () => {
    // 解決しない Promise でローディング状態を保持
    mockedFetchShops.mockReturnValue(new Promise<PagedResult<Shop>>(() => {}));

    renderPage();

    expect(screen.getByText('読み込み中...')).toBeInTheDocument();
  });

  it('キーワード入力でフィルタ付きで fetchShops を呼ぶ', async () => {
    const uev = userEvent.setup();
    mockedFetchShops.mockResolvedValue(makePagedResult([makeShop(1, '麺屋 いちばん')]));

    renderPage();

    await screen.findByText('麺屋 いちばん');

    await uev.type(screen.getByLabelText('キーワード'), 'ラーメン');

    await waitFor(() => {
      expect(mockedFetchShops).toHaveBeenCalledWith(
        expect.objectContaining({ q: 'ラーメン' }),
      );
    });
  });

  it('ジャンル選択でフィルタ付きで fetchShops を呼ぶ', async () => {
    const uev = userEvent.setup();
    mockedFetchShops.mockResolvedValue(makePagedResult([makeShop(1, '醤油ラーメン屋')]));

    renderPage();

    await screen.findByText('醤油ラーメン屋');

    await uev.selectOptions(
      screen.getByRole('combobox', { name: 'ジャンルで絞り込み' }),
      '醤油',
    );

    await waitFor(() => {
      expect(mockedFetchShops).toHaveBeenCalledWith(
        expect.objectContaining({ genre: '醤油' }),
      );
    });
  });

  describe('#search ハッシュ', () => {
    let scrollIntoViewMock: ReturnType<typeof vi.fn>;

    beforeEach(() => {
      scrollIntoViewMock = vi.fn();
      window.HTMLElement.prototype.scrollIntoView = scrollIntoViewMock;
    });

    afterEach(() => {
      // @ts-expect-error jsdom does not define scrollIntoView by default
      delete window.HTMLElement.prototype.scrollIntoView;
    });

    it('#search ハッシュで検索バーへスクロールする', async () => {
      mockedFetchShops.mockResolvedValue(makePagedResult([]));

      renderPage([{ pathname: '/', hash: '#search' }]);

      await waitFor(() => {
        expect(scrollIntoViewMock).toHaveBeenCalled();
      });
    });
  });
});
