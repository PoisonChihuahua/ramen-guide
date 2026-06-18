import { describe, it, expect, vi, beforeEach } from 'vitest';
import { register, login, fetchMe, logout } from './auth';
import { apiFetch } from './client';
import type { User } from '../types';

vi.mock('./client', () => ({ apiFetch: vi.fn() }));

const mockedApiFetch = vi.mocked(apiFetch);

const user: User = { id: 1, email: 'a@example.com', displayName: '太郎', role: 'User' };

describe('auth API', () => {
  beforeEach(() => {
    mockedApiFetch.mockReset();
  });

  it('register: POST /api/auth/register にメール・パスワード・表示名を送る', async () => {
    mockedApiFetch.mockResolvedValue(user);
    const result = await register('a@example.com', 'pass', '太郎');
    expect(mockedApiFetch).toHaveBeenCalledWith('/api/auth/register', {
      method: 'POST',
      body: { email: 'a@example.com', password: 'pass', displayName: '太郎' },
    });
    expect(result).toEqual(user);
  });

  it('login: POST /api/auth/login にメール・パスワードを送る', async () => {
    mockedApiFetch.mockResolvedValue(user);
    const result = await login('a@example.com', 'pass');
    expect(mockedApiFetch).toHaveBeenCalledWith('/api/auth/login', {
      method: 'POST',
      body: { email: 'a@example.com', password: 'pass' },
    });
    expect(result).toEqual(user);
  });

  it('fetchMe: GET /api/auth/me を呼ぶ', async () => {
    mockedApiFetch.mockResolvedValue(user);
    const result = await fetchMe();
    expect(mockedApiFetch).toHaveBeenCalledWith('/api/auth/me');
    expect(result).toEqual(user);
  });

  it('logout: POST /api/auth/logout を呼ぶ', async () => {
    mockedApiFetch.mockResolvedValue(undefined);
    await logout();
    expect(mockedApiFetch).toHaveBeenCalledWith('/api/auth/logout', { method: 'POST' });
  });
});
