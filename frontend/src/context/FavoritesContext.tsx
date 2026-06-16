import { useState, useEffect, useCallback, type ReactNode } from 'react';
import { FavoritesContext } from './favorites-context';
import { readFavorites, writeFavorites } from '../lib/favorites';

export function FavoritesProvider({ children }: { children: ReactNode }) {
  const [favoriteIds, setFavoriteIds] = useState<number[]>(() => readFavorites());

  // 変更のたびに localStorage へ永続化
  useEffect(() => {
    writeFavorites(favoriteIds);
  }, [favoriteIds]);

  const isFavorite = useCallback(
    (id: number) => favoriteIds.includes(id),
    [favoriteIds],
  );

  const toggleFavorite = useCallback((id: number) => {
    setFavoriteIds((prev) =>
      prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id],
    );
  }, []);

  return (
    <FavoritesContext.Provider value={{ favoriteIds, isFavorite, toggleFavorite }}>
      {children}
    </FavoritesContext.Provider>
  );
}
