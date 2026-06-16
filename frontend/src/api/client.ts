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

/**
 * 共通 fetch ラッパー。エラーハンドリングを一元化する。
 * 認証は httpOnly Cookie で行うため、常に credentials: 'include' で Cookie を送受信する
 * （JWT を JavaScript で保持しないことで XSS によるトークン窃取を防ぐ）。
 */
export async function apiFetch<T>(
  path: string,
  options: RequestOptions = {},
): Promise<T> {
  const { method = 'GET', body } = options;

  const headers: Record<string, string> = {};
  if (body !== undefined) {
    headers['Content-Type'] = 'application/json';
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    method,
    headers,
    credentials: 'include',
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });

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
