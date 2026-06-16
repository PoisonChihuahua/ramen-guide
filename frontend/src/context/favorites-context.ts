import { createContext } from 'react';

export interface FavoritesContextValue {
  favoriteIds: number[];
  isFavorite: (id: number) => boolean;
  toggleFavorite: (id: number) => void;
}

export const FavoritesContext = createContext<FavoritesContextValue | undefined>(
  undefined,
);
