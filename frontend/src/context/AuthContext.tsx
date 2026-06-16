import { useState, useEffect, useCallback, type ReactNode } from 'react';
import { AuthContext } from './auth-context';
import type { User } from '../types';
import * as authApi from '../api/auth';
import { AUTH_EXPIRED_EVENT } from '../api/client';

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // 起動時: httpOnly Cookie のセッションがあれば検証して復元する。
  // トークンは JS から見えないため、認証済みかどうかは /me の成否で判定する。
  useEffect(() => {
    authApi
      .fetchMe()
      .then(setUser)
      .catch(() => setUser(null))
      .finally(() => setIsLoading(false));
  }, []);

  // API クライアントがセッション失効を検知したら、ログイン状態を破棄する。
  // これにより、どの画面で 401 が継続してもログイン済み表示のまま固まらない。
  useEffect(() => {
    function handleAuthExpired() {
      setUser(null);
    }
    window.addEventListener(AUTH_EXPIRED_EVENT, handleAuthExpired);
    return () => window.removeEventListener(AUTH_EXPIRED_EVENT, handleAuthExpired);
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const loggedInUser = await authApi.login(email, password);
    setUser(loggedInUser);
  }, []);

  const register = useCallback(
    async (email: string, password: string, displayName: string) => {
      const registeredUser = await authApi.register(email, password, displayName);
      setUser(registeredUser);
    },
    [],
  );

  const logout = useCallback(async () => {
    await authApi.logout();
    setUser(null);
  }, []);

  return (
    <AuthContext.Provider value={{ user, isLoading, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  );
}
