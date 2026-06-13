import { Link, Outlet } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';

export function Layout() {
  const { user, logout } = useAuth();

  return (
    <div className="app">
      <header className="app-header">
        <Link to="/" className="app-logo">
          🍜 ラーメン図鑑
        </Link>
        <nav className="app-nav">
          {user ? (
            <>
              <span className="app-greeting">{user.displayName} さん</span>
              <button type="button" onClick={logout}>
                ログアウト
              </button>
            </>
          ) : (
            <>
              <Link to="/login">ログイン</Link>
              <Link to="/register">新規登録</Link>
            </>
          )}
        </nav>
      </header>
      <main className="app-main">
        <Outlet />
      </main>
      <footer className="app-footer">
        <small>© 2026 ラーメン図鑑</small>
      </footer>
    </div>
  );
}
