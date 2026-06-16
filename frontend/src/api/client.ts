const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5105';

export class ApiError extends Error {
  status: number;

  constructor(status: number, message: string) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
  }
}

interface RequestOptions {
  method?: string;
  body?: unknown;
}

// 自動リフレッシュの対象外（これら自体が 401 を返してもリフレッシュしない）
const NO_REFRESH_PATHS = [
  '/api/auth/login',
  '/api/auth/register',
  '/api/auth/refresh',
  '/api/auth/logout',
];

// 同時に複数の 401 が起きてもリフレッシュ要求は1回に集約する（single-flight）。
let refreshPromise: Promise<boolean> | null = null;

async function performRefresh(): Promise<boolean> {
  try {
    const response = await fetch(`${API_BASE_URL}/api/auth/refresh`, {
      method: 'POST',
      credentials: 'include',
    });
    return response.ok;
  } catch {
    return false;
  }
}

function refreshSession(): Promise<boolean> {
  if (!refreshPromise) {
    refreshPromise = performRefresh().finally(() => {
      refreshPromise = null;
    });
  }
  return refreshPromise;
}

/**
 * 共通 fetch ラッパー。エラーハンドリングを一元化する。
 * 認証は httpOnly Cookie で行うため、常に credentials: 'include' で Cookie を送受信する
 * （JWT を JavaScript で保持しないことで XSS によるトークン窃取を防ぐ）。
 * アクセストークン失効時（401）はリフレッシュ Cookie で一度だけ再発行を試み、再試行する。
 */
export async function apiFetch<T>(
  path: string,
  options: RequestOptions = {},
): Promise<T> {
  const { method = 'GET', body } = options;

  const sendRequest = (): Promise<Response> => {
    const headers: Record<string, string> = {};
    if (body !== undefined) {
      headers['Content-Type'] = 'application/json';
    }

    return fetch(`${API_BASE_URL}${path}`, {
      method,
      headers,
      credentials: 'include',
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
  };

  let response = await sendRequest();

  // アクセストークンが失効していたら、一度だけリフレッシュして再試行する。
  if (response.status === 401 && !NO_REFRESH_PATHS.includes(path)) {
    const refreshed = await refreshSession();
    if (refreshed) {
      response = await sendRequest();
    }
  }

  if (!response.ok) {
    const message = await extractErrorMessage(response);
    throw new ApiError(response.status, message);
  }

  // 204 No Content
  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

async function extractErrorMessage(response: Response): Promise<string> {
  try {
    const data = await response.json();
    if (data && typeof data.message === 'string') {
      return data.message;
    }
  } catch {
    // JSON でない場合は無視
  }
  return `エラーが発生しました (${response.status})`;
}
