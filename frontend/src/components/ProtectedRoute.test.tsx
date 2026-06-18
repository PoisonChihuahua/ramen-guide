import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { ProtectedRoute } from './ProtectedRoute';
import { useAuth } from '../hooks/useAuth';
import type { AuthContextValue } from '../context/auth-context';

vi.mock('../hooks/useAuth');
const mockedUseAuth = vi.mocked(useAuth);

function baseAuth(overrides: Partial<AuthContextValue>): AuthContextValue {
  return {
    user: null,
    isLoading: false,
    login: vi.fn(),
    register: vi.fn(),
    logout: vi.fn(),
    ...overrides,
  };
}

function renderRoute() {
  return render(
    <MemoryRouter initialEntries={['/mypage']}>
      <Routes>
        <Route element={<ProtectedRoute />}>
          <Route path="/mypage" element={<p>マイページ内容</p>} />
        </Route>
        <Route path="/login" element={<p>ログイン画面</p>} />
      </Routes>
    </MemoryRouter>,
  );
}

describe('ProtectedRoute', () => {
  beforeEach(() => {
    vi.resetAllMocks();
  });

  it('読み込み中はローディングを表示する', () => {
    mockedUseAuth.mockReturnValue(baseAuth({ isLoading: true }));

    renderRoute();

    expect(screen.getByText('読み込み中...')).toBeInTheDocument();
  });

  it('未ログインなら /login へリダイレクトする', () => {
    mockedUseAuth.mockReturnValue(baseAuth({ user: null, isLoading: false }));

    renderRoute();

    expect(screen.getByText('ログイン画面')).toBeInTheDocument();
    expect(screen.queryByText('マイページ内容')).not.toBeInTheDocument();
  });

  it('ログイン済みなら保護されたページを描画する', () => {
    mockedUseAuth.mockReturnValue(
      baseAuth({
        user: { id: 1, email: 'a@example.com', displayName: '太郎', role: 'User' },
        isLoading: false,
      }),
    );

    renderRoute();

    expect(screen.getByText('マイページ内容')).toBeInTheDocument();
  });
});
