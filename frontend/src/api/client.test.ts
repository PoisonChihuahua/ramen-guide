import { describe, it, expect, vi, afterEach } from 'vitest';
import { apiFetch, ApiError } from './client';

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

describe('apiFetch（httpOnly Cookie ＋ 自動リフレッシュ）', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('Cookie 送信のため常に credentials: include で呼び出す', async () => {
    const fetchMock = vi.fn(() =>
      Promise.resolve(jsonResponse({ id: 1, displayName: '太郎' })),
    );
    vi.stubGlobal('fetch', fetchMock);

    await apiFetch('/api/auth/me');

    const init = fetchMock.mock.calls[0][1] as RequestInit;
    expect(init.credentials).toBe('include');
  });

  it('401 のときリフレッシュして再試行する', async () => {
    let meCalls = 0;
    const fetchMock = vi.fn((url: string) => {
      if (url.includes('/api/auth/refresh')) {
        return Promise.resolve(new Response(null, { status: 204 }));
      }
      meCalls += 1;
      if (meCalls === 1) {
        return Promise.resolve(new Response('Unauthorized', { status: 401 }));
      }
      return Promise.resolve(jsonResponse({ id: 1, displayName: '太郎' }));
    });
    vi.stubGlobal('fetch', fetchMock);

    const result = await apiFetch<{ displayName: string }>('/api/auth/me');

    expect(result.displayName).toBe('太郎');
    // me(401) → refresh(204) → me(200) の3回
    expect(fetchMock).toHaveBeenCalledTimes(3);
    const refreshCalled = fetchMock.mock.calls.some((c) =>
      String(c[0]).includes('/api/auth/refresh'),
    );
    expect(refreshCalled).toBe(true);
  });

  it('リフレッシュに失敗したら 401 を投げる', async () => {
    const fetchMock = vi.fn(() =>
      Promise.resolve(new Response('Unauthorized', { status: 401 })),
    );
    vi.stubGlobal('fetch', fetchMock);

    await expect(apiFetch('/api/auth/me')).rejects.toBeInstanceOf(ApiError);
  });

  it('認証エンドポイント自身の 401 ではリフレッシュしない', async () => {
    const fetchMock = vi.fn(() =>
      Promise.resolve(
        jsonResponse({ message: 'メールアドレスまたはパスワードが正しくありません。' }, 401),
      ),
    );
    vi.stubGlobal('fetch', fetchMock);

    await expect(
      apiFetch('/api/auth/login', { method: 'POST', body: {} }),
    ).rejects.toMatchObject({ status: 401 });
    // login は NO_REFRESH のため1回のみ
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });
});
