import { useAuth } from '../hooks/useAuth';

/**
 * ログインユーザー専用のマイページ。ProtectedRoute 配下にあるため user は必ず存在する。
 */
export function MyPage() {
  const { user, logout } = useAuth();

  if (!user) {
    return null;
  }

  return (
    <main className="auth-page">
      <div className="auth-card">
        <h1 className="auth-title">マイページ</h1>
        <p className="auth-sub">{user.displayName} さん、ようこそ。</p>
        <dl className="profile-list">
          <div className="profile-row">
            <dt>表示名</dt>
            <dd>{user.displayName}</dd>
          </div>
          <div className="profile-row">
            <dt>メールアドレス</dt>
            <dd>{user.email}</dd>
          </div>
        </dl>
        <button className="auth-button" type="button" onClick={logout}>
          ログアウト
        </button>
      </div>
    </main>
  );
}
