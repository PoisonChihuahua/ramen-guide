import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';

interface ProtectedRouteProps {
  /** true の場合、管理者ロールを必須にする。 */
  requireAdmin?: boolean;
}

/**
 * 認証が必要なルートを保護する。未ログインなら /login へリダイレクトし、
 * ログイン後に元のページへ戻れるよう遷移元を state に保持する。
 * requireAdmin 指定時は管理者ロールも要求する。
 */
export function ProtectedRoute({ requireAdmin = false }: ProtectedRouteProps) {
  const { user, isLoading } = useAuth();
  const location = useLocation();

  if (isLoading) {
    return <div className="route-loading">読み込み中...</div>;
  }

  if (!user) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  if (requireAdmin && user.role !== 'Admin') {
    return (
      <main className="state-message state-error">
        このページにアクセスする権限がありません。
      </main>
    );
  }

  return <Outlet />;
}
