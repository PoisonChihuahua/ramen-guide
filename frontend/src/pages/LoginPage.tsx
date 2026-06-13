import { useState, type FormEvent } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { ApiError } from '../api/client';

export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await login(email, password);
      navigate('/');
    } catch (err) {
      setError(
        err instanceof ApiError ? err.message : 'ログインに失敗しました。',
      );
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <main className="auth-page">
      <div className="auth-card">
        <h1 className="auth-title">ログイン</h1>
        <p className="auth-sub">ラーメン図鑑へおかえりなさい。</p>
        <form className="auth-form" onSubmit={handleSubmit}>
          {error && <p className="form-error">{error}</p>}
          <div className="form-group">
            <label className="form-label" htmlFor="login-email">
              メールアドレス
            </label>
            <input
              id="login-email"
              className="form-input"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </div>
          <div className="form-group">
            <label className="form-label" htmlFor="login-password">
              パスワード
            </label>
            <input
              id="login-password"
              className="form-input"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />
          </div>
          <button className="auth-button" type="submit" disabled={submitting}>
            {submitting ? '送信中...' : 'ログイン'}
          </button>
        </form>
        <p className="auth-switch">
          アカウントをお持ちでない方は <Link to="/register">新規登録</Link>
        </p>
      </div>
    </main>
  );
}
