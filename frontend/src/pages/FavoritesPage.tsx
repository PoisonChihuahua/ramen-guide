import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { fetchFavorites } from '../api/favorites';
import { ShopCard } from '../components/ShopCard';

/** ログインユーザーのお気に入り店舗一覧（サーバー保存・ユーザー単位）。 */
export function FavoritesPage() {
  const {
    data: shops,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ['favorites'],
    queryFn: fetchFavorites,
  });

  return (
    <main className="shop-list-page">
      <section className="list-section">
        <div className="section-head">
          <div>
            <h2 className="section-title">お気に入り</h2>
            <p className="section-sub">Favorites</p>
          </div>
          {shops && <p className="section-count">全 {shops.length} 店舗</p>}
        </div>

        {isLoading && <p className="state-message">読み込み中...</p>}
        {isError && (
          <p className="state-message state-error">
            お気に入りの取得に失敗しました。
          </p>
        )}
        {shops && shops.length === 0 && (
          <p className="state-message">
            まだお気に入りがありません。気になるお店を{' '}
            <Link to="/" className="back-link">
              一覧
            </Link>{' '}
            から登録してみましょう。
          </p>
        )}

        {shops && shops.length > 0 && (
          <div className="shop-grid">
            {shops.map((shop) => (
              <ShopCard key={shop.id} shop={shop} />
            ))}
          </div>
        )}
      </section>
    </main>
  );
}
