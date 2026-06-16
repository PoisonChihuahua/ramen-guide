import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AuthProvider } from './AuthContext';
import { useAuth } from '../hooks/useAuth';
import * as authApi from '../api/auth';
import type { User } from '../types';

// API レイヤをモックし、AuthProvider の状態管理だけを検証する。
// 認証トークンは httpOnly Cookie で扱うため、ここではユーザー状態と API 呼び出しのみを確認する。
vi.mock('../api/auth');
const mockedAuthApi = vi.mocked(authApi);

const user: User = { id: 1, email: 'taro@example.com', displayName: '太郎' };

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
    vi.resetAllMocks();
    // 既定では未ログイン（/me が 401）として振る舞う
    mockedAuthApi.fetchMe.mockRejectedValue(new Error('unauthorized'));
  });

  it('セッションが無いときはゲスト状態で初期化する', async () => {
    renderProvider();

    expect(await screen.findByTestId('who')).toHaveTextContent('ゲスト');
    expect(mockedAuthApi.fetchMe).toHaveBeenCalled();
  });

  it('起動時に Cookie セッションがあればユーザーを復元する', async () => {
    mockedAuthApi.fetchMe.mockResolvedValue(user);

    renderProvider();

    expect(await screen.findByTestId('who')).toHaveTextContent('太郎');
  });

  it('login 成功でユーザーを保存する', async () => {
    const u = userEvent.setup();
    mockedAuthApi.login.mockResolvedValue(user);
    renderProvider();
    await screen.findByTestId('who');

    await u.click(screen.getByRole('button', { name: 'login' }));

    expect(await screen.findByTestId('who')).toHaveTextContent('太郎');
    expect(mockedAuthApi.login).toHaveBeenCalledWith('taro@example.com', 'password123');
  });

  it('register 成功でユーザーを保存する', async () => {
    const u = userEvent.setup();
    mockedAuthApi.register.mockResolvedValue(user);
    renderProvider();
    await screen.findByTestId('who');

    await u.click(screen.getByRole('button', { name: 'register' }));

    expect(await screen.findByTestId('who')).toHaveTextContent('太郎');
  });

  it('logout でユーザーを破棄し、サーバ側のログアウトを呼ぶ', async () => {
    const u = userEvent.setup();
    mockedAuthApi.fetchMe.mockResolvedValue(user);
    mockedAuthApi.logout.mockResolvedValue(undefined);
    renderProvider();
    expect(await screen.findByTestId('who')).toHaveTextContent('太郎');

    await u.click(screen.getByRole('button', { name: 'logout' }));

    expect(await screen.findByTestId('who')).toHaveTextContent('ゲスト');
    expect(mockedAuthApi.logout).toHaveBeenCalled();
  });
});
