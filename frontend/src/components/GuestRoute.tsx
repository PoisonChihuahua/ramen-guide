import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';

/**
 * 未ログインユーザー専用のルート（ログイン・新規登録）。
 * 既にログイン済みならトップへリダイレクトする。
 */
export function GuestRoute() {
  const { user, isLoading } = useAuth();

  if (isLoading) {
    return <div className="route-loading">読み込み中...</div>;
  }

  if (user) {
    return <Navigate to="/" replace />;
  }

  return <Outlet />;
}
