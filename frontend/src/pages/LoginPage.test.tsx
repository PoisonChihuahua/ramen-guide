import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { LoginPage } from './LoginPage';
import { useAuth } from '../hooks/useAuth';
import { ApiError } from '../api/client';
import type { AuthContextValue } from '../context/auth-context';

vi.mock('../hooks/useAuth');
const mockedUseAuth = vi.mocked(useAuth);

const login = vi.fn();

function authValue(): AuthContextValue {
  return {
    user: null,
    isLoading: false,
    login,
    register: vi.fn(),
    logout: vi.fn(),
  };
}

function renderLogin() {
  return render(
    <MemoryRouter initialEntries={['/login']}>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/" element={<p>ホーム画面</p>} />
      </Routes>
    </MemoryRouter>,
  );
}

describe('LoginPage', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    mockedUseAuth.mockReturnValue(authValue());
  });

  it('正しい入力でログインし、ホームへ遷移する', async () => {
    const u = userEvent.setup();
    login.mockResolvedValue(undefined);
    renderLogin();

    await u.type(screen.getByLabelText('メールアドレス'), 'taro@example.com');
    await u.type(screen.getByLabelText('パスワード'), 'password123');
    await u.click(screen.getByRole('button', { name: 'ログイン' }));

    expect(login).toHaveBeenCalledWith('taro@example.com', 'password123');
    expect(await screen.findByText('ホーム画面')).toBeInTheDocument();
  });

  it('認証に失敗するとエラーメッセージを表示する', async () => {
    const u = userEvent.setup();
    login.mockRejectedValue(
      new ApiError(401, 'メールアドレスまたはパスワードが正しくありません。'),
    );
    renderLogin();

    await u.type(screen.getByLabelText('メールアドレス'), 'taro@example.com');
    await u.type(screen.getByLabelText('パスワード'), 'wrong-pass');
    await u.click(screen.getByRole('button', { name: 'ログイン' }));

    expect(
      await screen.findByText('メールアドレスまたはパスワードが正しくありません。'),
    ).toBeInTheDocument();
    // 遷移していない
    expect(screen.queryByText('ホーム画面')).not.toBeInTheDocument();
  });

  it('想定外のエラーでも汎用メッセージを表示する', async () => {
    const u = userEvent.setup();
    login.mockRejectedValue(new Error('network down'));
    renderLogin();

    await u.type(screen.getByLabelText('メールアドレス'), 'taro@example.com');
    await u.type(screen.getByLabelText('パスワード'), 'password123');
    await u.click(screen.getByRole('button', { name: 'ログイン' }));

    expect(await screen.findByText('ログインに失敗しました。')).toBeInTheDocument();
  });
});
