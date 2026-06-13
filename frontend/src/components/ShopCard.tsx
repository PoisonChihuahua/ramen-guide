import { Link } from 'react-router-dom';
import type { Shop } from '../types';

export function ShopCard({ shop }: { shop: Shop }) {
  return (
    <Link to={`/shops/${shop.id}`} className="shop-card">
      <img
        src={shop.imageUrl}
        alt={shop.name}
        className="shop-card-image"
        loading="lazy"
      />
      <div className="shop-card-body">
        <h3 className="shop-card-name">{shop.name}</h3>
        <div className="shop-card-tags">
          <span className="tag tag-genre">{shop.genre}</span>
          <span className="tag tag-area">{shop.area}</span>
        </div>
        <p className="shop-card-desc">{shop.description}</p>
        <p className="shop-card-price">{shop.priceRange}</p>
      </div>
    </Link>
  );
}
