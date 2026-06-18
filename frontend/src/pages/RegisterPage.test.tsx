import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { RegisterPage } from './RegisterPage';
import { useAuth } from '../hooks/useAuth';
import { ApiError } from '../api/client';
import type { AuthContextValue } from '../context/auth-context';

vi.mock('../hooks/useAuth');
const mockedUseAuth = vi.mocked(useAuth);

const register = vi.fn();

function authValue(): AuthContextValue {
  return {
    user: null,
    isLoading: false,
    login: vi.fn(),
    register,
    logout: vi.fn(),
  };
}

function renderRegister() {
  return render(
    <MemoryRouter initialEntries={['/register']}>
      <Routes>
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/" element={<p>ホーム画面</p>} />
      </Routes>
    </MemoryRouter>,
  );
}

describe('RegisterPage', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    mockedUseAuth.mockReturnValue(authValue());
  });

  it('入力値で登録し、ホームへ遷移する', async () => {
    const u = userEvent.setup();
    register.mockResolvedValue(undefined);
    renderRegister();

    await u.type(screen.getByLabelText('表示名'), '新規太郎');
    await u.type(screen.getByLabelText('メールアドレス'), 'shinki@example.com');
    await u.type(
      screen.getByLabelText('パスワード（8文字以上）'),
      'password123',
    );
    await u.click(screen.getByRole('button', { name: '登録する' }));

    expect(register).toHaveBeenCalledWith(
      'shinki@example.com',
      'password123',
      '新規太郎',
    );
    expect(await screen.findByText('ホーム画面')).toBeInTheDocument();
  });

  it('メール重複などのエラーでメッセージを表示する', async () => {
    const u = userEvent.setup();
    register.mockRejectedValue(
      new ApiError(409, 'このメールアドレスは既に登録されています。'),
    );
    renderRegister();

    await u.type(screen.getByLabelText('表示名'), '新規太郎');
    await u.type(screen.getByLabelText('メールアドレス'), 'dup@example.com');
    await u.type(
      screen.getByLabelText('パスワード（8文字以上）'),
      'password123',
    );
    await u.click(screen.getByRole('button', { name: '登録する' }));

    expect(
      await screen.findByText('このメールアドレスは既に登録されています。'),
    ).toBeInTheDocument();
    expect(screen.queryByText('ホーム画面')).not.toBeInTheDocument();
  });
});
