import { useContext } from 'react';
import { FavoritesContext } from '../context/favorites-context';

export function useFavorites() {
  const ctx = useContext(FavoritesContext);
  if (!ctx) {
    throw new Error(
      'useFavorites は FavoritesProvider の内側で使用してください。',
    );
  }
  return ctx;
}
