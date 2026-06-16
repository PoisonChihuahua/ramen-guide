import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MyPage } from './MyPage';
import { useAuth } from '../hooks/useAuth';
import type { User } from '../types';

vi.mock('../hooks/useAuth');
const mockedUseAuth = vi.mocked(useAuth);

const user: User = { id: 1, email: 'taro@example.com', displayName: '太郎', role: 'User' };

describe('MyPage', () => {
  it('ユーザー情報（表示名・メール）を描画する', () => {
    mockedUseAuth.mockReturnValue({
      user,
      isLoading: false,
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
    });
    render(<MyPage />);
    expect(screen.getByText('太郎 さん、ようこそ。')).toBeInTheDocument();
    expect(screen.getByText('太郎')).toBeInTheDocument();
    expect(screen.getByText('taro@example.com')).toBeInTheDocument();
  });

  it('user が null のとき何も描画しない', () => {
    mockedUseAuth.mockReturnValue({
      user: null,
      isLoading: false,
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
    });
    const { container } = render(<MyPage />);
    expect(container).toBeEmptyDOMElement();
  });

  it('ログアウトボタンをクリックすると logout が呼ばれる', async () => {
    const u = userEvent.setup();
    const logoutFn = vi.fn();
    mockedUseAuth.mockReturnValue({
      user,
      isLoading: false,
      login: vi.fn(),
      register: vi.fn(),
      logout: logoutFn,
    });
    render(<MyPage />);
    await u.click(screen.getByRole('button', { name: 'ログアウト' }));
    expect(logoutFn).toHaveBeenCalled();
  });
});
