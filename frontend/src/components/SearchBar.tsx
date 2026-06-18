import type { FormEvent } from 'react';
import type { ShopFilters } from '../types';

// API からジャンル/エリア候補を取得できないとき用のフォールバック
const DEFAULT_GENRES = ['醤油', '味噌', '豚骨', '塩', '豚骨醤油'];
const DEFAULT_AREAS = ['札幌', '東京', '横浜', '博多'];

interface SearchBarProps {
  filters: ShopFilters;
  onChange: (filters: ShopFilters) => void;
  /** 店舗データから算出した絞り込み候補。未指定・空配列なら既定値を使う。 */
  genres?: string[];
  areas?: string[];
}

function SearchIcon() {
  return (
    <svg
      className="ic"
      viewBox="0 0 18 18"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      aria-hidden="true"
    >
      <circle cx="8" cy="8" r="5.5" />
      <path d="M12.5 12.5L16 16" />
    </svg>
  );
}

export function SearchBar({ filters, onChange, genres, areas }: SearchBarProps) {
  const genreOptions = genres && genres.length > 0 ? genres : DEFAULT_GENRES;
  const areaOptions = areas && areas.length > 0 ? areas : DEFAULT_AREAS;

  // 絞り込みは入力のたびにライブで反映されるため、送信はデフォルト動作を抑止するだけ
  function handleSubmit(e: FormEvent) {
    e.preventDefault();
  }

  return (
    <form className="search-bar" onSubmit={handleSubmit} role="search">
      <div className="search-field search-field--input">
        <input
          className="search-input"
          type="search"
          aria-label="キーワード"
          placeholder="店名・特徴で探す（例：濃厚、あっさり）"
          value={filters.q ?? ''}
          onChange={(e) => onChange({ ...filters, q: e.target.value })}
        />
      </div>
      <div className="search-field search-field--select">
        <select
          className="search-select"
          aria-label="ジャンルで絞り込み"
          value={filters.genre ?? ''}
          onChange={(e) =>
            onChange({ ...filters, genre: e.target.value || undefined })
          }
        >
          <option value="">ジャンル：すべて</option>
          {genreOptions.map((g) => (
            <option key={g} value={g}>
              {g}
            </option>
          ))}
        </select>
      </div>
      <div className="search-field search-field--select">
        <select
          className="search-select"
          aria-label="エリアで絞り込み"
          value={filters.area ?? ''}
          onChange={(e) =>
            onChange({ ...filters, area: e.target.value || undefined })
          }
        >
          <option value="">エリア：すべて</option>
          {areaOptions.map((a) => (
            <option key={a} value={a}>
              {a}
            </option>
          ))}
        </select>
      </div>
      <button className="search-button" type="submit">
        <SearchIcon /> 検索
      </button>
    </form>
  );
}
