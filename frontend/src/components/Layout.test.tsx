import { describe, it, expect, vi } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { Layout } from './Layout';
import { useAuth } from '../hooks/useAuth';
import type { User } from '../types';

vi.mock('../hooks/useAuth');
const mockedUseAuth = vi.mocked(useAuth);

const guestAuth = { user: null, isLoading: false, login: vi.fn(), register: vi.fn(), logout: vi.fn() };
const userAuth = (user: User) => ({ user, isLoading: false, login: vi.fn(), register: vi.fn(), logout: vi.fn() });

function renderLayout() {
  return render(
    <MemoryRouter>
      <Routes>
        <Route element={<Layout />}>
          <Route index element={<div>コンテンツ</div>} />
        </Route>
      </Routes>
    </MemoryRouter>,
  );
}

describe('Layout', () => {
  it('ゲスト時はヘッダーナビにログイン・新規登録リンクを表示する', () => {
    mockedUseAuth.mockReturnValue(guestAuth);
    renderLayout();
    const nav = screen.getByRole('navigation', { name: 'メイン' });
    expect(within(nav).getByRole('link', { name: 'ログイン' })).toBeInTheDocument();
    expect(within(nav).getByRole('link', { name: '新規登録' })).toBeInTheDocument();
    expect(within(nav).queryByText('お気に入り')).not.toBeInTheDocument();
  });

  it('ログイン済みユーザーには表示名とお気に入りリンクを表示する', () => {
    mockedUseAuth.mockReturnValue(
      userAuth({ id: 1, email: 'a@example.com', displayName: '太郎', role: 'User' }),
    );
    renderLayout();
    const nav = screen.getByRole('navigation', { name: 'メイン' });
    expect(screen.getByText('太郎 さん')).toBeInTheDocument();
    expect(within(nav).getByRole('link', { name: 'お気に入り' })).toBeInTheDocument();
    expect(within(nav).queryByRole('link', { name: 'ログイン' })).not.toBeInTheDocument();
  });

  it('管理者には店舗管理リンクを表示する', () => {
    mockedUseAuth.mockReturnValue(
      userAuth({ id: 2, email: 'admin@example.com', displayName: '管理者', role: 'Admin' }),
    );
    renderLayout();
    const nav = screen.getByRole('navigation', { name: 'メイン' });
    expect(within(nav).getByRole('link', { name: '店舗管理' })).toBeInTheDocument();
  });

  it('一般ユーザーには店舗管理リンクを表示しない', () => {
    mockedUseAuth.mockReturnValue(
      userAuth({ id: 1, email: 'a@example.com', displayName: '太郎', role: 'User' }),
    );
    renderLayout();
    const nav = screen.getByRole('navigation', { name: 'メイン' });
    expect(within(nav).queryByRole('link', { name: '店舗管理' })).not.toBeInTheDocument();
  });

  it('ログアウトボタンをクリックすると logout が呼ばれる', async () => {
    const u = userEvent.setup();
    const logoutFn = vi.fn();
    mockedUseAuth.mockReturnValue({
      user: { id: 1, email: 'a@example.com', displayName: '太郎', role: 'User' },
      isLoading: false,
      login: vi.fn(),
      register: vi.fn(),
      logout: logoutFn,
    });
    renderLayout();
    await u.click(screen.getByRole('button', { name: 'ログアウト' }));
    expect(logoutFn).toHaveBeenCalled();
  });

  it('フッターを描画する', () => {
    mockedUseAuth.mockReturnValue(guestAuth);
    renderLayout();
    expect(screen.getAllByText(/ラーメン図鑑/).length).toBeGreaterThan(0);
  });
});
