import { apiFetch } from './client';
import type { User } from '../types';

// 認証トークンは httpOnly Cookie でやり取りされるため、レスポンスボディはユーザー情報のみ。
// アクセストークンの失効時は client.ts が /api/auth/refresh を自動的に呼び出して再発行する。

export function register(
  email: string,
  password: string,
  displayName: string,
): Promise<User> {
  return apiFetch<User>('/api/auth/register', {
    method: 'POST',
    body: { email, password, displayName },
  });
}

export function login(email: string, password: string): Promise<User> {
  return apiFetch<User>('/api/auth/login', {
    method: 'POST',
    body: { email, password },
  });
}

export function fetchMe(): Promise<User> {
  return apiFetch<User>('/api/auth/me');
}

export function logout(): Promise<void> {
  return apiFetch<void>('/api/auth/logout', { method: 'POST' });
}
