import { Link, Outlet } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';

export function Layout() {
  const { user, logout } = useAuth();

  return (
    <div className="layout theme-noren">
      <header className="site-header">
        <div className="header-inner">
          <Link className="logo" to="/" aria-label="ラーメン図鑑 ホーム">
            <span className="logo__mark" aria-hidden="true">
              🍜
            </span>
            <span className="logo__name">ラーメン図鑑</span>
            <span className="logo__sub">Ramen Zukan</span>
          </Link>
          <nav className="nav" aria-label="メイン">
            <Link className="nav-link" to="/">
              店舗一覧
            </Link>
            <Link className="nav-link" to="/ask">
              AI検索
            </Link>
            {user ? (
              <>
                <span className="nav-greeting">{user.displayName} さん</span>
                <button className="nav-link" type="button" onClick={logout}>
                  ログアウト
                </button>
              </>
            ) : (
              <>
                <Link className="nav-link" to="/login">
                  ログイン
                </Link>
                <Link className="nav-link nav-link--primary" to="/register">
                  新規登録
                </Link>
              </>
            )}
          </nav>
        </div>
        <div className="noren-band" aria-hidden="true">
          <span className="noren-band__text">ら ー め ん 図 鑑</span>
        </div>
      </header>

      <Outlet />

      <footer className="site-footer">
        <div className="footer-inner">
          <div>
            <div className="footer-brand">
              <span aria-hidden="true">🍜</span> ラーメン図鑑{' '}
              <span className="logo__sub">Ramen Zukan</span>
            </div>
            <p className="footer-note">
              全国のラーメン店を、写真とともに紹介するガイド。きょうの気分とエリアから、あなたの一杯を見つけてください。
            </p>
          </div>
          <div className="footer-cols">
            <div className="footer-col">
              <h4>探す</h4>
              <Link to="/">店舗一覧</Link>
              <Link to="/">ジャンルから</Link>
              <Link to="/">エリアから</Link>
            </div>
            <div className="footer-col">
              <h4>アカウント</h4>
              <Link to="/login">ログイン</Link>
              <Link to="/register">新規登録</Link>
              <Link to="/">お気に入り</Link>
            </div>
            <div className="footer-col">
              <h4>サイト情報</h4>
              <Link to="/">このサイトについて</Link>
              <Link to="/">お問い合わせ</Link>
              <Link to="/">利用規約</Link>
            </div>
          </div>
        </div>
        <div className="footer-bar">
          <div className="footer-bar-inner">
            <span>© 2026 ラーメン図鑑</span>
            <span>made with 🍜 in Japan</span>
          </div>
        </div>
      </footer>
    </div>
  );
}
