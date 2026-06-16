import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ReviewSection } from './ReviewSection';
import * as reviewsApi from '../api/reviews';
import { useAuth } from '../hooks/useAuth';
import type { Review, User } from '../types';

vi.mock('../api/reviews');
vi.mock('../hooks/useAuth');

const mockedUseAuth = vi.mocked(useAuth);
const mockedReviews = vi.mocked(reviewsApi);

const user: User = {
  id: 7,
  email: 'u@example.com',
  displayName: 'レビュアー',
  role: 'User',
};

function setAuth(value: User | null) {
  mockedUseAuth.mockReturnValue({
    user: value,
    isLoading: false,
    login: vi.fn(),
    register: vi.fn(),
    logout: vi.fn(),
  });
}

function makeReview(over: Partial<Review> = {}): Review {
  return {
    id: 1,
    shopId: 3,
    userId: 99,
    displayName: '太郎',
    rating: 5,
    comment: 'スープが最高でした。',
    createdAt: '2026-06-01T00:00:00Z',
    updatedAt: '2026-06-01T00:00:00Z',
    ...over,
  };
}

function renderSection() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ReviewSection shopId={3} />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ReviewSection', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('取得したレビューを一覧表示する', async () => {
    setAuth(null);
    mockedReviews.fetchReviews.mockResolvedValue([
      makeReview({ id: 1, displayName: '太郎', comment: 'スープが最高でした。' }),
    ]);

    renderSection();

    expect(await screen.findByText('スープが最高でした。')).toBeInTheDocument();
    expect(screen.getByText('太郎')).toBeInTheDocument();
  });

  it('未ログイン時は投稿フォームの代わりにログイン誘導を出す', async () => {
    setAuth(null);
    mockedReviews.fetchReviews.mockResolvedValue([]);

    renderSection();

    expect(
      await screen.findByText(/ログイン/, { selector: 'a' }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: '投稿する' }),
    ).not.toBeInTheDocument();
  });

  it('ログイン時に星とコメントを入力して投稿APIを呼ぶ', async () => {
    const uev = userEvent.setup();
    setAuth(user);
    mockedReviews.fetchReviews.mockResolvedValue([]);
    mockedReviews.submitReview.mockResolvedValue(
      makeReview({ userId: 7, rating: 4, comment: 'また来ます' }),
    );

    renderSection();

    await uev.click(await screen.findByRole('radio', { name: '星4つ' }));
    await uev.type(screen.getByLabelText('コメント'), 'また来ます');
    await uev.click(screen.getByRole('button', { name: '投稿する' }));

    expect(mockedReviews.submitReview).toHaveBeenCalledWith(3, {
      rating: 4,
      comment: 'また来ます',
    });
  });
});
