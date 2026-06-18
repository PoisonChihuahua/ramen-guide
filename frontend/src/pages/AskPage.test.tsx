import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AskPage } from './AskPage';
import { askShops } from '../api/rag';
import type { AskResponse } from '../types';

// API レイヤをモックし、ページの描画振る舞いだけを検証する
vi.mock('../api/rag', () => ({
  askShops: vi.fn(),
}));

const mockedAskShops = vi.mocked(askShops);

function makeResponse(): AskResponse {
  return {
    question: '味噌ラーメン',
    answer: '「札幌味噌堂」がおすすめです。',
    matches: [
      {
        shop: {
          id: 2,
          name: '札幌味噌堂',
          description: 'コクのある味噌ラーメン。',
          address: '北海道札幌市',
          area: '札幌',
          genre: '味噌',
          openingHours: '11:00〜22:00',
          priceRange: '1000〜1500円',
          imageUrl: 'https://example.com/x.jpg',
          averageRating: 0,
          reviewCount: 0,
        },
        score: 0.42,
      },
    ],
  };
}

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <AskPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('AskPage', () => {
  beforeEach(() => {
    mockedAskShops.mockReset();
  });

  it('質問を送信すると回答と関連店舗を表示する', async () => {
    const user = userEvent.setup();
    mockedAskShops.mockResolvedValue(makeResponse());

    renderPage();

    await user.type(screen.getByLabelText('質問'), '味噌ラーメン');
    await user.click(screen.getByRole('button', { name: '検索する' }));

    expect(
      await screen.findByText('「札幌味噌堂」がおすすめです。'),
    ).toBeInTheDocument();
    expect(screen.getByText('札幌味噌堂')).toBeInTheDocument();
    expect(mockedAskShops).toHaveBeenCalledWith('味噌ラーメン');
  });

  it('入力が空のときは送信ボタンが無効', () => {
    renderPage();

    expect(screen.getByRole('button', { name: '検索する' })).toBeDisabled();
  });
});
