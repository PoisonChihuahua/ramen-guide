export interface Shop {
  id: number;
  name: string;
  description: string;
  address: string;
  area: string;
  genre: string;
  openingHours: string;
  priceRange: string;
  imageUrl: string;
  averageRating: number;
  reviewCount: number;
}

/** 店舗の作成・更新フォーム入力（管理者用）。 */
export interface ShopInput {
  name: string;
  description: string;
  address: string;
  area: string;
  genre: string;
  openingHours: string;
  priceRange: string;
  imageUrl: string;
}

export type UserRole = 'User' | 'Admin';

export interface User {
  id: number;
  email: string;
  displayName: string;
  role: UserRole;
}

export interface Review {
  id: number;
  shopId: number;
  userId: number;
  displayName: string;
  rating: number;
  comment: string;
  createdAt: string;
  updatedAt: string;
}

export interface ReviewInput {
  rating: number;
  comment: string;
}

export interface FavoriteStatus {
  shopId: number;
  isFavorite: boolean;
}

export interface ShopFilters {
  q?: string;
  genre?: string;
  area?: string;
}

/** 自然文検索（RAG）で引いた店舗1件と、その類似度スコア。 */
export interface ShopMatch {
  shop: Shop;
  score: number;
}

/** 自然文検索（RAG）のレスポンス。 */
export interface AskResponse {
  question: string;
  answer: string;
  matches: ShopMatch[];
}
