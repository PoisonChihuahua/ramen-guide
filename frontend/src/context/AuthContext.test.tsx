import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AuthProvider } from './AuthContext';
import { useAuth } from '../hooks/useAuth';
import * as authApi from '../api/auth';
import { setTokens, getToken, getRefreshToken } from '../api/client';
import type { AuthResponse, User } from '../types';

// API レイヤをモックし、AuthProvider の状態管理だけを検証する
vi.mock('../api/auth');
const mockedAuthApi = vi.mocked(authApi);

const user: User = { id: 1, email: 'taro@example.com', displayName: '太郎' };
const authResponse: AuthResponse = {
  token: 'access-token',
  refreshToken: 'refresh-token',
  user,
};

function Consumer() {
  const { user, isLoading, login, register, logout } = useAuth();
  if (isLoading) {
    return <p>読み込み中</p>;
  }
  return (
    <div>
      <p data-testid="who">{user ? user.displayName : 'ゲスト'}</p>
      <button type="button" onClick={() => login('taro@example.com', 'password123')}>
        login
      </button>
      <button
        type="button"
        onClick={() => register('taro@example.com', 'password123', '太郎')}
      >
        register
      </button>
      <button type="button" onClick={logout}>
        logout
      </button>
    </div>
  );
}

function renderProvider() {
  return render(
    <AuthProvider>
      <Consumer />
    </AuthProvider>,
  );
}

describe('AuthContext', () => {
  beforeEach(() => {
    localStorage.clear();
    vi.resetAllMocks();
  });

  it('トークンが無いときはゲスト状態で初期化し、me を呼ばない', async () => {
    renderProvider();

    expect(await screen.findByTestId('who')).toHaveTextContent('ゲスト');
    expect(mockedAuthApi.fetchMe).not.toHaveBeenCalled();
  });

  it('login 成功でユーザーとトークンを保存する', async () => {
    const u = userEvent.setup();
    mockedAuthApi.login.mockResolvedValue(authResponse);
    renderProvider();
    await screen.findByTestId('who');

    await u.click(screen.getByRole('button', { name: 'login' }));

    expect(await screen.findByTestId('who')).toHaveTextContent('太郎');
    expect(mockedAuthApi.login).toHaveBeenCalledWith('taro@example.com', 'password123');
    expect(getToken()).toBe('access-token');
    expect(getRefreshToken()).toBe('refresh-token');
  });

  it('register 成功でユーザーとトークンを保存する', async () => {
    const u = userEvent.setup();
    mockedAuthApi.register.mockResolvedValue(authResponse);
    renderProvider();
    await screen.findByTestId('who');

    await u.click(screen.getByRole('button', { name: 'register' }));

    expect(await screen.findByTestId('who')).toHaveTextContent('太郎');
    expect(getToken()).toBe('access-token');
  });

  it('起動時に保存済みトークンを検証してユーザーを復元する', async () => {
    setTokens('access-token', 'refresh-token');
    mockedAuthApi.fetchMe.mockResolvedValue(user);

    renderProvider();

    expect(await screen.findByTestId('who')).toHaveTextContent('太郎');
    expect(mockedAuthApi.fetchMe).toHaveBeenCalled();
  });

  it('起動時に me が失敗したらトークンを破棄してゲストにする', async () => {
    setTokens('stale-token', 'stale-refresh');
    mockedAuthApi.fetchMe.mockRejectedValue(new Error('unauthorized'));

    renderProvider();

    expect(await screen.findByTestId('who')).toHaveTextContent('ゲスト');
    await waitFor(() => expect(getToken()).toBeNull());
    expect(getRefreshToken()).toBeNull();
  });

  it('logout でトークンを破棄し、サーバ側の失効を呼ぶ', async () => {
    const u = userEvent.setup();
    setTokens('access-token', 'refresh-token');
    mockedAuthApi.fetchMe.mockResolvedValue(user);
    mockedAuthApi.logout.mockResolvedValue(undefined);
    renderProvider();
    expect(await screen.findByTestId('who')).toHaveTextContent('太郎');

    await u.click(screen.getByRole('button', { name: 'logout' }));

    expect(await screen.findByTestId('who')).toHaveTextContent('ゲスト');
    expect(getToken()).toBeNull();
    expect(mockedAuthApi.logout).toHaveBeenCalledWith('refresh-token');
  });
});
