import { useState } from 'react';
import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { SearchBar } from './SearchBar';
import type { ShopFilters } from '../types';

/** 実アプリと同じ controlled フローを再現する検証用ハーネス。 */
function Harness() {
  const [filters, setFilters] = useState<ShopFilters>({});
  return (
    <>
      <SearchBar filters={filters} onChange={setFilters} />
      <output data-testid="filters">{JSON.stringify(filters)}</output>
    </>
  );
}

function currentFilters(): ShopFilters {
  return JSON.parse(screen.getByTestId('filters').textContent || '{}');
}

describe('SearchBar', () => {
  it('キーワード入力が絞り込み条件 q に反映される', async () => {
    const user = userEvent.setup();
    render(<Harness />);

    const input = screen.getByLabelText('キーワード');
    await user.type(input, '濃厚');

    expect(currentFilters().q).toBe('濃厚');
  });

  it('ジャンル選択が絞り込み条件 genre に反映される', async () => {
    const user = userEvent.setup();
    render(<Harness />);

    await user.selectOptions(screen.getByLabelText('ジャンルで絞り込み'), '味噌');

    expect(currentFilters().genre).toBe('味噌');
  });

  it('エリア選択が絞り込み条件 area に反映される', async () => {
    const user = userEvent.setup();
    render(<Harness />);

    await user.selectOptions(screen.getByLabelText('エリアで絞り込み'), '札幌');

    expect(currentFilters().area).toBe('札幌');
  });

  it('「すべて」を選ぶと該当条件が undefined に戻る', async () => {
    const user = userEvent.setup();
    render(<Harness />);
    const genreSelect = screen.getByLabelText('ジャンルで絞り込み');

    await user.selectOptions(genreSelect, '味噌');
    expect(currentFilters().genre).toBe('味噌');

    await user.selectOptions(genreSelect, 'ジャンル：すべて');
    expect(currentFilters().genre).toBeUndefined();
  });
});
