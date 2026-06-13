import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { ShopCard } from './ShopCard';
import type { Shop } from '../types';

const shop: Shop = {
  id: 42,
  name: '麺屋 テスト',
  description: '濃厚な豚骨醤油の一杯。',
  address: '東京都千代田区1-1-1',
  area: '東京',
  genre: '豚骨醤油',
  openingHours: '11:00〜22:00',
  priceRange: '¥800〜¥1,200',
  imageUrl: 'https://example.com/ramen.jpg',
};

function renderCard() {
  return render(
    <MemoryRouter>
      <ShopCard shop={shop} />
    </MemoryRouter>,
  );
}

describe('ShopCard', () => {
  it('店名・ジャンル・エリア・価格・説明を表示する', () => {
    renderCard();

    expect(screen.getByText('麺屋 テスト')).toBeInTheDocument();
    expect(screen.getByText('豚骨醤油')).toBeInTheDocument();
    expect(screen.getByText('濃厚な豚骨醤油の一杯。')).toBeInTheDocument();
    expect(screen.getByText('¥800〜¥1,200')).toBeInTheDocument();
    // エリアはタグと area-flag の2箇所に出る
    expect(screen.getAllByText('東京').length).toBeGreaterThan(0);
  });

  it('詳細ページ /shops/:id へのリンクになっている', () => {
    renderCard();

    const link = screen.getByRole('link', { name: /麺屋 テスト/ });
    expect(link).toHaveAttribute('href', '/shops/42');
  });

  it('店舗写真に店名の代替テキストが付く', () => {
    renderCard();

    const img = screen.getByAltText('麺屋 テスト');
    expect(img).toHaveAttribute('src', 'https://example.com/ramen.jpg');
  });
});
