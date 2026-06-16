import { useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useAuth } from '../hooks/useAuth';
import {
  addFavorite,
  fetchFavoriteStatus,
  removeFavorite,
} from '../api/favorites';

interface FavoriteButtonProps {
  shopId: number;
}

/**
 * お気に入り追加/解除ボタン。
 * 未ログイン時はログインページへ誘導する。
 */
export function FavoriteButton({ shopId }: FavoriteButtonProps) {
  const { user } = useAuth();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const { data: status } = useQuery({
    queryKey: ['favorite-status', shopId],
    queryFn: () => fetchFavoriteStatus(shopId),
    enabled: Boolean(user),
  });

  const isFavorite = status?.isFavorite ?? false;

  const mutation = useMutation({
    mutationFn: () =>
      isFavorite ? removeFavorite(shopId) : addFavorite(shopId),
    onSuccess: (result) => {
      queryClient.setQueryData(['favorite-status', shopId], result);
      queryClient.invalidateQueries({ queryKey: ['favorites'] });
    },
  });

  function handleClick() {
    if (!user) {
      navigate('/login');
      return;
    }
    mutation.mutate();
  }

  const label = !user
    ? 'お気に入りに追加'
    : isFavorite
      ? 'お気に入り解除'
      : 'お気に入りに追加';

  return (
    <button
      className={`info-btn ${isFavorite ? 'info-btn--fav-on' : 'info-btn--ghost'}`}
      type="button"
      onClick={handleClick}
      disabled={mutation.isPending}
      aria-pressed={isFavorite}
    >
      <span aria-hidden="true">{isFavorite ? '♥' : '♡'}</span>
      {mutation.isPending ? '更新中...' : label}
    </button>
  );
}
