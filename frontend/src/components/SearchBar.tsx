import type { ShopFilters } from '../types';

const GENRES = ['醤油', '味噌', '豚骨', '塩', '豚骨醤油'];
const AREAS = ['札幌', '東京', '横浜', '博多'];

interface SearchBarProps {
  filters: ShopFilters;
  onChange: (filters: ShopFilters) => void;
}

export function SearchBar({ filters, onChange }: SearchBarProps) {
  return (
    <div className="search-bar">
      <input
        type="search"
        className="search-input"
        placeholder="店名・エリアで検索"
        value={filters.q ?? ''}
        onChange={(e) => onChange({ ...filters, q: e.target.value })}
      />
      <select
        className="search-select"
        value={filters.genre ?? ''}
        onChange={(e) =>
          onChange({ ...filters, genre: e.target.value || undefined })
        }
      >
        <option value="">ジャンル: すべて</option>
        {GENRES.map((g) => (
          <option key={g} value={g}>
            {g}
          </option>
        ))}
      </select>
      <select
        className="search-select"
        value={filters.area ?? ''}
        onChange={(e) =>
          onChange({ ...filters, area: e.target.value || undefined })
        }
      >
        <option value="">エリア: すべて</option>
        {AREAS.map((a) => (
          <option key={a} value={a}>
            {a}
          </option>
        ))}
      </select>
    </div>
  );
}
