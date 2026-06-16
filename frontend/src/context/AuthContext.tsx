import { useState, useEffect, useCallback, type ReactNode } from 'react';
import { AuthContext } from './auth-context';
import type { User } from '../types';
import * as authApi from '../api/auth';

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
