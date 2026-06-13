import { useState, type FormEvent } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { ApiError } from '../api/client';

export function RegisterPage() {
  const { register } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await register(email, password, displayName);
      navigate('/');
    } catch (err) {
      setError(err instanceof ApiError ? err.message : '登録に失敗しました。');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <main className="auth-page">
      <div className="auth-card">
        <h1 className="auth-title">新規登録</h1>
        <p className="auth-sub">あなたの一杯を、見つけにいこう。</p>
        <form className="auth-form" onSubmit={handleSubmit}>
          {error && <p className="form-error">{error}</p>}
          <div className="form-group">
            <label className="form-label" htmlFor="reg-name">
              表示名
            </label>
            <input
              id="reg-name"
              className="form-input"
              type="text"
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
              maxLength={50}
              required
            />
          </div>
          <div className="form-group">
            <label className="form-label" htmlFor="reg-email">
              メールアドレス
            </label>
            <input
              id="reg-email"
              className="form-input"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </div>
          <div className="form-group">
            <label className="form-label" htmlFor="reg-password">
              パスワード（8文字以上）
            </label>
            <input
              id="reg-password"
              className="form-input"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              minLength={8}
              required
            />
          </div>
          <button className="auth-button" type="submit" disabled={submitting}>
            {submitting ? '送信中...' : '登録する'}
          </button>
        </form>
        <p className="auth-switch">
          既にアカウントをお持ちの方は <Link to="/login">ログイン</Link>
        </p>
      </div>
    </main>
  );
}
