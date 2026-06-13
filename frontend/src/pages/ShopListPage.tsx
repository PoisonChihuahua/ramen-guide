import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { fetchShops } from '../api/shops';
import { ShopCard } from '../components/ShopCard';
import { SearchBar } from '../components/SearchBar';
import type { ShopFilters } from '../types';

const HERO_IMAGE =
  'https://images.unsplash.com/photo-1557872943-16a5ac26437e?w=1600&q=80';

export function ShopListPage() {
  const [filters, setFilters] = useState<ShopFilters>({});

  const {
    data: shops,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ['shops', filters],
    queryFn: () => fetchShops(filters),
  });

  return (
    <main className="shop-list-page">
      <section className="hero">
        <div className="hero__media">
          <img className="media-img" src={HERO_IMAGE} alt="" aria-hidden="true" />
        </div>
        <div className="hero__overlay" aria-hidden="true"></div>
        <div className="hero__inner">
          <span className="hero__eyebrow">全国のラーメンを、一杯ずつ</span>
          <h1 className="hero__title">
            きょうの一杯に、
            <br />
            <span className="accent">出会う。</span>
          </h1>
          <p className="hero__lead">
            日本各地の名店を写真とともに。ジャンルとエリアから、あなた好みの一杯を見つけてください。
          </p>
          <div className="hero__search">
            <SearchBar filters={filters} onChange={setFilters} />
          </div>
        </div>
      </section>

      <section className="list-section">
        <div className="section-head">
          <div>
            <h2 className="section-title">店舗一覧</h2>
            <p className="section-sub">All Shops</p>
          </div>
          {shops && <p className="section-count">全 {shops.length} 店舗</p>}
        </div>

        {isLoading && <p className="state-message">読み込み中...</p>}
        {isError && (
          <p className="state-message state-error">
            店舗情報の取得に失敗しました。
          </p>
        )}
        {shops && shops.length === 0 && (
          <p className="state-message">
            条件に一致する店舗が見つかりませんでした。
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
