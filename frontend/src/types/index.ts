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
  refreshToken: string;
  user: User;
}

export interface ShopFilters {
  q?: string;
  genre?: string;
  area?: string;
}
