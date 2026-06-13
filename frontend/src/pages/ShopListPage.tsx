import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { fetchShops } from '../api/shops';
import { ShopCard } from '../components/ShopCard';
import { SearchBar } from '../components/SearchBar';
import type { ShopFilters } from '../types';

export function ShopListPage() {
  const [filters, setFilters] = useState<ShopFilters>({});

  const { data: shops, isLoading, isError } = useQuery({
    queryKey: ['shops', filters],
    queryFn: () => fetchShops(filters),
  });

  return (
    <div className="shop-list-page">
      <section className="page-hero">
        <h1>お気に入りの一杯を見つけよう</h1>
        <p>全国のラーメン店をジャンル・エリアで探せます。</p>
      </section>

      <SearchBar filters={filters} onChange={setFilters} />

      {isLoading && <p className="state-message">読み込み中...</p>}
      {isError && (
        <p className="state-message state-error">
          店舗情報の取得に失敗しました。
        </p>
      )}
      {shops && shops.length === 0 && (
        <p className="state-message">条件に一致する店舗が見つかりませんでした。</p>
      )}

      {shops && shops.length > 0 && (
        <div className="shop-grid">
          {shops.map((shop) => (
            <ShopCard key={shop.id} shop={shop} />
          ))}
        </div>
      )}
    </div>
  );
}
