import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AdminShopFormPage } from './AdminShopFormPage';
import { createShop, fetchShop, updateShop } from '../api/shops';
import type { Shop } from '../types';

vi.mock('../api/shops', () => ({
  createShop: vi.fn(),
  fetchShop: vi.fn(),
  updateShop: vi.fn(),
}));

const mockedCreateShop = vi.mocked(createShop);
const mockedFetchShop = vi.mocked(fetchShop);
const mockedUpdateShop = vi.mocked(updateShop);

const shop: Shop = {
  id: 1,
  name: '麺屋テスト',
  description: 'テスト説明',
  address: '東京都1-1-1',
  area: '東京',
  genre: '醤油',
  openingHours: '11:00〜22:00',
  priceRange: '¥800〜',
  imageUrl: 'https://example.com/x.jpg',
  averageRating: 0,
  reviewCount: 0,
};

function renderNewForm() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/admin/shops/new']}>
        <Routes>
          <Route path="/admin/shops/new" element={<AdminShopFormPage />} />
          <Route path="/admin" element={<div>管理画面</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

function renderEditForm(id = 1) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/admin/shops/${id}/edit`]}>
        <Routes>
          <Route path="/admin/shops/:id/edit" element={<AdminShopFormPage />} />
          <Route path="/admin" element={<div>管理画面</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('AdminShopFormPage', () => {
  beforeEach(() => {
    mockedCreateShop.mockReset();
    mockedFetchShop.mockReset();
    mockedUpdateShop.mockReset();
  });

  describe('新規追加フォーム', () => {
    it('「店舗を新規追加」ヘッダーを表示する', () => {
      renderNewForm();
      expect(screen.getByText('店舗を新規追加')).toBeInTheDocument();
    });

    it('「追加する」ボタンを表示する', () => {
      renderNewForm();
      expect(screen.getByRole('button', { name: '追加する' })).toBeInTheDocument();
    });

    it('フォーム送信で createShop を呼びリダイレクトする', async () => {
      const u = userEvent.setup();
      mockedCreateShop.mockResolvedValue(shop);
      renderNewForm();

      await u.type(screen.getByLabelText('店名'), '麺屋テスト');
      await u.type(screen.getByLabelText('説明'), 'テスト説明');
      await u.type(screen.getByLabelText('住所'), '東京都1-1-1');
      await u.type(screen.getByLabelText('エリア'), '東京');
      await u.type(screen.getByLabelText('ジャンル'), '醤油');
      await u.type(screen.getByLabelText('営業時間'), '11:00〜22:00');
      await u.type(screen.getByLabelText('価格帯'), '¥800〜');
      await u.type(screen.getByLabelText('画像URL'), 'https://example.com/x.jpg');
      await u.click(screen.getByRole('button', { name: '追加する' }));

      await waitFor(() => expect(mockedCreateShop).toHaveBeenCalled());
      expect(await screen.findByText('管理画面')).toBeInTheDocument();
    });
  });

  describe('編集フォーム', () => {
    it('既存店舗を取得する間はローディングを表示する', () => {
      mockedFetchShop.mockReturnValue(new Promise<Shop>(() => {}));
      renderEditForm();
      expect(screen.getByText('読み込み中...')).toBeInTheDocument();
    });

    it('「店舗を編集」ヘッダーを表示する', async () => {
      mockedFetchShop.mockResolvedValue(shop);
      renderEditForm();
      expect(await screen.findByText('店舗を編集')).toBeInTheDocument();
    });

    it('既存データをフォームに初期値として表示する', async () => {
      mockedFetchShop.mockResolvedValue(shop);
      renderEditForm();
      const nameInput = (await screen.findByLabelText('店名')) as HTMLInputElement;
      expect(nameInput.value).toBe('麺屋テスト');
    });

    it('フォーム送信で updateShop を呼びリダイレクトする', async () => {
      const u = userEvent.setup();
      mockedFetchShop.mockResolvedValue(shop);
      mockedUpdateShop.mockResolvedValue(shop);
      renderEditForm();

      await screen.findByLabelText('店名');
      await u.click(screen.getByRole('button', { name: '更新する' }));

      await waitFor(() => expect(mockedUpdateShop).toHaveBeenCalledWith(1, expect.any(Object)));
      expect(await screen.findByText('管理画面')).toBeInTheDocument();
    });
  });
});
