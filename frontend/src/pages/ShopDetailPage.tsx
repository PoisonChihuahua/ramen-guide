import { useParams, Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { fetchShop } from '../api/shops';

// ジャンル → プレースホルダーの出汁トーン
const GENRE_TONE: Record<string, string> = {
  豚骨醤油: 'tonkotsu-shoyu',
  味噌: 'miso',
  醤油: 'shoyu',
  豚骨: 'tonkotsu',
  塩: 'shio',
};

export function ShopDetailPage() {
  const { id } = useParams<{ id: string }>();
  const shopId = Number(id);

  const {
    data: shop,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ['shop', shopId],
    queryFn: () => fetchShop(shopId),
    enabled: Number.isFinite(shopId),
  });

  if (isLoading) return <main className="state-message">読み込み中...</main>;
  if (isError || !shop)
    return (
      <main className="state-message state-error">
        <p>店舗が見つかりませんでした。</p>
        <Link to="/" className="back-link">
          一覧に戻る
        </Link>
      </main>
    );

  const tone = GENRE_TONE[shop.genre] ?? 'shoyu';

  return (
    <main className="shop-detail-page">
      <nav className="detail-breadcrumb" aria-label="パンくず">
        <Link to="/">ホーム</Link>
        <span className="sep">›</span>
        <Link to="/">店舗一覧</Link>
        <span className="sep">›</span>
        <span aria-current="page">{shop.name}</span>
      </nav>

      <section className="detail-hero">
        <div className="detail-hero__media">
          <img className="media-img" src={shop.imageUrl} alt={shop.name} />
          <div className="detail-hero__scrim" aria-hidden="true"></div>
        </div>
        <div className="detail-hero__caption">
          <div className="detail-hero__tags">
            <span className="tag tag--genre">{shop.genre}</span>
            <span className="tag tag--area">{shop.area}</span>
          </div>
          <h1 className="detail-title">{shop.name}</h1>
          <p className="detail-tagline">
            {shop.area}で味わう、{shop.genre}の一杯。
          </p>
        </div>
      </section>

      <div className="detail-body">
        <div className="detail-main">
          <section className="detail-section">
            <h2>このお店について</h2>
            <p className="detail-lead">{shop.description}</p>
            <p className="detail-lead">
              スープ・麺・トッピングそれぞれにこだわりが詰まった一杯。席数はカウンター中心で、ひとりでも気軽に立ち寄れます。行列ができることもあるので、時間に余裕をもって訪れるのがおすすめです。
            </p>
          </section>
          <section className="detail-section detail-gallery">
            <h2>ギャラリー</h2>
            <div className="gallery-grid">
              <img className="media-img" src={shop.imageUrl} alt={`${shop.name} の一杯`} />
              <div className={`ph ph--tonkotsu`} role="img" aria-label="店内（準備中）">
                <span className="ph__tag">店内</span>
              </div>
              <div className={`ph ph--${tone}`} role="img" aria-label="トッピング（準備中）">
                <span className="ph__tag">トッピング</span>
              </div>
            </div>
          </section>
        </div>

        <aside className="detail-side">
          <div className="info-card">
            <div className="info-card__head">店舗情報</div>
            <div className="info-list">
              <div className="info-row">
                <span className="info-label">Genre / ジャンル</span>
                <span className="info-value">{shop.genre}</span>
              </div>
              <div className="info-row">
                <span className="info-label">Area / エリア</span>
                <span className="info-value">{shop.area}</span>
              </div>
              <div className="info-row">
                <span className="info-label">Address / 住所</span>
                <span className="info-value">{shop.address}</span>
              </div>
              <div className="info-row">
                <span className="info-label">Hours / 営業時間</span>
                <span className="info-value">{shop.openingHours}</span>
              </div>
              <div className="info-row">
                <span className="info-label">Price / 価格帯</span>
                <span className="info-value price">{shop.priceRange}</span>
              </div>
            </div>
            <div className="info-actions">
              <button className="info-btn info-btn--primary" type="button">
                地図で見る
              </button>
              <button className="info-btn info-btn--ghost" type="button">
                お気に入りに追加
              </button>
            </div>
          </div>
        </aside>
      </div>
    </main>
  );
}
