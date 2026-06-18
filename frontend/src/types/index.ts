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
}

export interface User {
  id: number;
  email: string;
  displayName: string;
}

export interface AuthResponse {
  token: string;
  user: User;
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
