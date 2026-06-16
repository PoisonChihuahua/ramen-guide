import { Link } from 'react-router-dom';
import { StarRating } from './StarRating';
import type { Shop } from '../types';

export function ShopCard({ shop }: { shop: Shop }) {
  return (
    <Link to={`/shops/${shop.id}`} className="shop-card">
      <div className="shop-card__media">
        <img
          className="media-img"
          src={shop.imageUrl}
          alt={shop.name}
          loading="lazy"
        />
        <span className="shop-card__area-flag">{shop.area}</span>
      </div>
      <div className="shop-card__body">
        <div className="shop-card__tags">
          <span className="tag tag--genre">{shop.genre}</span>
          <span className="tag tag--area">{shop.area}</span>
        </div>
        <h3 className="shop-card__name">{shop.name}</h3>
        {shop.reviewCount > 0 && (
          <div className="shop-card__rating">
            <StarRating
              value={shop.averageRating}
              count={shop.reviewCount}
              size="sm"
            />
          </div>
        )}
        <p className="shop-card__desc">{shop.description}</p>
        <div className="shop-card__foot">
          <span className="shop-card__price">
            <small>価格帯</small>
            {shop.priceRange}
          </span>
          <span className="shop-card__more">
            詳しく見る
            <svg
              width="14"
              height="14"
              viewBox="0 0 14 14"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
              aria-hidden="true"
            >
              <path d="M3 7h8M7.5 3.5L11 7l-3.5 3.5" />
            </svg>
          </span>
        </div>
      </div>
    </Link>
  );
}
