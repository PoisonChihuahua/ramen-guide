import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { fetchShops } from '../api/shops';
import { ShopCard } from '../components/ShopCard';
import { useFavorites } from '../hooks/useFavorites';

export function FavoritesPage() {
  const { favoriteIds } = useFavorites();

  const {
    data: shops,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ['shops', {}],
    queryFn: () => fetchShops(),
  });

  const favoriteShops = (shops ?? []).filter((shop) =>
    favoriteIds.includes(shop.id),
  );

  return (
    <main className="shop-list-page">
      <section className="list-section">
        <div className="section-head">
          <div>
            <h2 className="section-title">お気に入り</h2>
            <p className="section-sub">Favorites</p>
          </div>
          {shops && <p className="section-count">全 {favoriteShops.length} 店舗</p>}
        </div>

        {isLoading && <p className="state-message">読み込み中...</p>}
        {isError && (
          <p className="state-message state-error">
            店舗情報の取得に失敗しました。
          </p>
        )}
        {!isLoading && !isError && favoriteShops.length === 0 && (
          <p className="state-message">
            お気に入りに登録した店舗はまだありません。
            <br />
            <Link to="/" className="back-link">
              店舗一覧から探す
            </Link>
          </p>
        )}

        {favoriteShops.length > 0 && (
          <div className="shop-grid">
            {favoriteShops.map((shop) => (
              <ShopCard key={shop.id} shop={shop} />
            ))}
          </div>
        )}
      </section>
    </main>
  );
}
