import { useParams, Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { fetchShop } from '../api/shops';

export function ShopDetailPage() {
  const { id } = useParams<{ id: string }>();
  const shopId = Number(id);

  const { data: shop, isLoading, isError } = useQuery({
    queryKey: ['shop', shopId],
    queryFn: () => fetchShop(shopId),
    enabled: Number.isFinite(shopId),
  });

  if (isLoading) return <p className="state-message">読み込み中...</p>;
  if (isError || !shop)
    return (
      <div className="state-message state-error">
        <p>店舗が見つかりませんでした。</p>
        <Link to="/">一覧に戻る</Link>
      </div>
    );

  return (
    <article className="shop-detail">
      <Link to="/" className="back-link">
        ← 一覧に戻る
      </Link>
      <img src={shop.imageUrl} alt={shop.name} className="shop-detail-image" />
      <h1 className="shop-detail-name">{shop.name}</h1>
      <div className="shop-card-tags">
        <span className="tag tag-genre">{shop.genre}</span>
        <span className="tag tag-area">{shop.area}</span>
      </div>
      <p className="shop-detail-desc">{shop.description}</p>
      <dl className="shop-detail-info">
        <dt>住所</dt>
        <dd>{shop.address}</dd>
        <dt>営業時間</dt>
        <dd>{shop.openingHours}</dd>
        <dt>価格帯</dt>
        <dd>{shop.priceRange}</dd>
      </dl>
    </article>
  );
}
