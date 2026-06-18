import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { FavoriteButton } from './FavoriteButton';
import * as favoritesApi from '../api/favorites';
import { useAuth } from '../hooks/useAuth';
import type { User } from '../types';

vi.mock('../api/favorites');
vi.mock('../hooks/useAuth');

const navigateMock = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>(
    'react-router-dom',
  );
  return { ...actual, useNavigate: () => navigateMock };
});

const mockedUseAuth = vi.mocked(useAuth);
const mockedFavorites = vi.mocked(favoritesApi);

const user: User = {
  id: 1,
  email: 'u@example.com',
  displayName: 'テスト',
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

function renderButton() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <FavoriteButton shopId={42} />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('FavoriteButton', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('未ログイン時にクリックするとログインページへ誘導する', async () => {
    const uev = userEvent.setup();
    setAuth(null);

    renderButton();
    await uev.click(screen.getByRole('button', { name: /お気に入りに追加/ }));

    expect(navigateMock).toHaveBeenCalledWith('/login');
    expect(mockedFavorites.addFavorite).not.toHaveBeenCalled();
  });

  it('ログイン済みでクリックするとお気に入り登録APIを呼ぶ', async () => {
    const uev = userEvent.setup();
    setAuth(user);
    mockedFavorites.fetchFavoriteStatus.mockResolvedValue({
      shopId: 42,
      isFavorite: false,
    });
    mockedFavorites.addFavorite.mockResolvedValue({
      shopId: 42,
      isFavorite: true,
    });

    renderButton();
    await uev.click(screen.getByRole('button', { name: /お気に入りに追加/ }));

    await waitFor(() =>
      expect(mockedFavorites.addFavorite).toHaveBeenCalledWith(42),
    );
  });

  it('既にお気に入りなら解除ラベルを表示する', async () => {
    setAuth(user);
    mockedFavorites.fetchFavoriteStatus.mockResolvedValue({
      shopId: 42,
      isFavorite: true,
    });

    renderButton();

    expect(
      await screen.findByRole('button', { name: /お気に入り解除/ }),
    ).toHaveAttribute('aria-pressed', 'true');
  });
});
