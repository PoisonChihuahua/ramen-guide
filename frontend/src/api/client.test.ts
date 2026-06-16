import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import {
  apiFetch,
  ApiError,
  setTokens,
  getToken,
  getRefreshToken,
} from './client';

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

describe('apiFetch のトークン自動リフレッシュ', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('認証リクエストが 401 のときリフレッシュして再試行する', async () => {
    setTokens('expired-access', 'valid-refresh');

    let meCalls = 0;
    const fetchMock = vi.fn((url: string) => {
      if (url.includes('/api/auth/refresh')) {
        return Promise.resolve(
          jsonResponse({ token: 'new-access', refreshToken: 'new-refresh' }),
        );
      }
      meCalls += 1;
      if (meCalls === 1) {
        return Promise.resolve(new Response('Unauthorized', { status: 401 }));
      }
      return Promise.resolve(
        jsonResponse({ id: 1, email: 'a@example.com', displayName: '太郎' }),
      );
    });
    vi.stubGlobal('fetch', fetchMock);

    const result = await apiFetch<{ displayName: string }>('/api/auth/me', {
      auth: true,
    });

    expect(result.displayName).toBe('太郎');
    // 新しいトークンが保存されている
    expect(getToken()).toBe('new-access');
    expect(getRefreshToken()).toBe('new-refresh');
    // 再試行時は新しいアクセストークンを付与している
    const retryCall = fetchMock.mock.calls.at(-1);
    const retryHeaders = (retryCall?.[1] as RequestInit).headers as Record<
      string,
      string
    >;
    expect(retryHeaders['Authorization']).toBe('Bearer new-access');
  });

  it('リフレッシュに失敗したらトークンを破棄して 401 を投げる', async () => {
    setTokens('expired-access', 'invalid-refresh');

    const fetchMock = vi.fn((url: string) => {
      if (url.includes('/api/auth/refresh')) {
        return Promise.resolve(new Response('Unauthorized', { status: 401 }));
      }
      return Promise.resolve(new Response('Unauthorized', { status: 401 }));
    });
    vi.stubGlobal('fetch', fetchMock);

    await expect(apiFetch('/api/auth/me', { auth: true })).rejects.toMatchObject({
      status: 401,
    });
    await expect(
      apiFetch('/api/auth/me', { auth: true }),
    ).rejects.toBeInstanceOf(ApiError);

    expect(getToken()).toBeNull();
    expect(getRefreshToken()).toBeNull();
  });

  it('リフレッシュトークンが無ければリフレッシュを試みない', async () => {
    const fetchMock = vi.fn(() =>
      Promise.resolve(new Response('Unauthorized', { status: 401 })),
    );
    vi.stubGlobal('fetch', fetchMock);

    await expect(apiFetch('/api/auth/me', { auth: true })).rejects.toBeInstanceOf(
      ApiError,
    );
    // 401 → リフレッシュトークンが無いので1回のみ
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });
});
