import { useState, useEffect, useCallback, type ReactNode } from 'react';
import { AuthContext } from './auth-context';
import type { User } from '../types';
import * as authApi from '../api/auth';
import { getToken, getRefreshToken, setTokens, clearTokens } from '../api/client';

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  // トークンが無ければ復元処理は不要なので、最初からローディングしない
  const [isLoading, setIsLoading] = useState(() => getToken() !== null);

  // 起動時: トークンがあれば検証して復元（失効していれば自動リフレッシュされる）
  useEffect(() => {
    if (!getToken()) {
      return;
    }
    let active = true;
    authApi
      .fetchMe()
      .then((restored) => {
        if (active) {
          setUser(restored);
        }
      })
      .catch(() => clearTokens())
      .finally(() => {
        if (active) {
          setIsLoading(false);
        }
      });
    return () => {
      active = false;
    };
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const res = await authApi.login(email, password);
    setTokens(res.token, res.refreshToken);
    setUser(res.user);
  }, []);

  const register = useCallback(
    async (email: string, password: string, displayName: string) => {
      const res = await authApi.register(email, password, displayName);
      setTokens(res.token, res.refreshToken);
      setUser(res.user);
    },
    [],
  );

  const logout = useCallback(() => {
    const refreshToken = getRefreshToken();
    clearTokens();
    setUser(null);
    // サーバ側のリフレッシュトークンも失効させる（ベストエフォート）
    if (refreshToken) {
      authApi.logout(refreshToken).catch(() => {
        /* ローカルは既にクリア済みのため失敗は無視 */
      });
    }
  }, []);

  return (
    <AuthContext.Provider value={{ user, isLoading, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  );
}
