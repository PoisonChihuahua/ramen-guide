import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Outlet, Route, Routes } from 'react-router-dom';
import { GuestRoute } from './GuestRoute';
import { useAuth } from '../hooks/useAuth';
import type { User } from '../types';

vi.mock('../hooks/useAuth');
const mockedUseAuth = vi.mocked(useAuth);

const user: User = { id: 1, email: 'a@example.com', displayName: '太郎', role: 'User' };

function renderRoute(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route element={<GuestRoute />}>
          <Route path="/login" element={<div>ログインページ</div>} />
        </Route>
        <Route path="/" element={<div>ホーム</div>} />
      </Routes>
    </MemoryRouter>,
  );
}

describe('GuestRoute', () => {
  it('読み込み中はローディングを表示する', () => {
    mockedUseAuth.mockReturnValue({
      user: null,
      isLoading: true,
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
    });
    renderRoute('/login');
    expect(screen.getByText('読み込み中...')).toBeInTheDocument();
  });

  it('未ログインならアウトレットを描画する', () => {
    mockedUseAuth.mockReturnValue({
      user: null,
      isLoading: false,
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
    });
    renderRoute('/login');
    expect(screen.getByText('ログインページ')).toBeInTheDocument();
  });

  it('ログイン済みなら / へリダイレクトする', () => {
    mockedUseAuth.mockReturnValue({
      user,
      isLoading: false,
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
    });
    renderRoute('/login');
    expect(screen.getByText('ホーム')).toBeInTheDocument();
    expect(screen.queryByText('ログインページ')).not.toBeInTheDocument();
  });
});
