import { useEffect, useRef } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useSearchParams, useLocation } from 'react-router-dom';
import { fetchShops, fetchShopOptions } from '../api/shops';
import { ShopCard } from '../components/ShopCard';
import { SearchBar } from '../components/SearchBar';
import type { ShopFilters } from '../types';

const HERO_IMAGE =
  'https://images.unsplash.com/photo-1557872943-16a5ac26437e?w=1600&q=80';

function paramsToFilters(params: URLSearchParams): ShopFilters {
  return {
    q: params.get('q') || undefined,
    genre: params.get('genre') || undefined,
    area: params.get('area') || undefined,
    page: params.get('page') ? Number(params.get('page')) : undefined,
  };
}

export function ShopListPage() {
  // 絞り込み条件は URL クエリで保持し、再読込・共有でも復元できるようにする
  const [searchParams, setSearchParams] = useSearchParams();
  const filters = paramsToFilters(searchParams);
  const location = useLocation();
  const searchRef = useRef<HTMLDivElement>(null);

  function handleFilterChange(next: ShopFilters) {
    const params = new URLSearchParams();
    if (next.q) params.set('q', next.q);
    if (next.genre) params.set('genre', next.genre);
    if (next.area) params.set('area', next.area);
    // フィルタ変更時はページを 1 にリセット
    setSearchParams(params, { replace: true });
  }

  function handlePageChange(newPage: number) {
    const params = new URLSearchParams(searchParams);
    params.set('page', String(newPage));
    setSearchParams(params, { replace: true });
  }

  const {
    data: result,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ['shops', filters],
    queryFn: () => fetchShops(filters),
  });

  // 絞り込み候補はフィルタとは独立して全件から取得する
  const { data: shopOptions } = useQuery({
    queryKey: ['shopOptions'],
    queryFn: fetchShopOptions,
    staleTime: Infinity,
  });
  const genres = shopOptions?.genres ?? [];
  const areas = shopOptions?.areas ?? [];

  // フッターの「#search」リンクから来たら検索バーへスクロール
  useEffect(() => {
    if (location.hash === '#search') {
      searchRef.current?.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
  }, [location.hash]);

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
          <div className="hero__search" id="search" ref={searchRef}>
            <SearchBar
              filters={filters}
              onChange={handleFilterChange}
              genres={genres}
              areas={areas}
            />
          </div>
        </div>
      </section>

      <section className="list-section">
        <div className="section-head">
          <div>
            <h2 className="section-title">店舗一覧</h2>
            <p className="section-sub">All Shops</p>
          </div>
          {result && <p className="section-count">全 {result.total} 店舗</p>}
        </div>

        {isLoading && <p className="state-message">読み込み中...</p>}
        {isError && (
          <p className="state-message state-error">
            店舗情報の取得に失敗しました。
          </p>
        )}
        {result && result.items.length === 0 && (
          <p className="state-message">
            条件に一致する店舗が見つかりませんでした。
          </p>
        )}

        {result && result.items.length > 0 && (
          <div className="shop-grid">
            {result.items.map((shop) => (
              <ShopCard key={shop.id} shop={shop} />
            ))}
          </div>
        )}

        {result && Math.ceil(result.total / result.limit) > 1 && (
          <div className="pagination">
            <button
              className="pagination__btn"
              disabled={result.page <= 1}
              onClick={() => handlePageChange(result.page - 1)}
            >
              前へ
            </button>
            <span className="pagination__info">
              {result.page} / {Math.ceil(result.total / result.limit)} ページ
            </span>
            <button
              className="pagination__btn"
              disabled={result.page >= Math.ceil(result.total / result.limit)}
              onClick={() => handlePageChange(result.page + 1)}
            >
              次へ
            </button>
          </div>
        )}
      </section>
    </main>
  );
}
