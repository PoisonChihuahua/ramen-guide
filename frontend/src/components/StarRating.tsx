import { useState } from 'react';

const MAX_STARS = 5;
const STAR_VALUES = [1, 2, 3, 4, 5] as const;

interface StarRatingProps {
  /** 0〜5 の評価値。小数も可（表示用の塗りに反映）。 */
  value: number;
  /** 件数（表示する場合）。 */
  count?: number;
  /** サイズ調整用の追加クラス。 */
  size?: 'sm' | 'md' | 'lg';
}

/** 読み取り専用の星評価表示。小数評価は部分塗りで表現する。 */
export function StarRating({ value, count, size = 'md' }: StarRatingProps) {
  const clamped = Math.max(0, Math.min(MAX_STARS, value));
  const percent = (clamped / MAX_STARS) * 100;
  const label =
    count !== undefined
      ? `5段階評価で ${clamped.toFixed(1)}（${count}件のレビュー）`
      : `5段階評価で ${clamped.toFixed(1)}`;

  return (
    <span className={`star-rating star-rating--${size}`} role="img" aria-label={label}>
      <span className="star-rating__track" aria-hidden="true">
        <span className="star-rating__base">★★★★★</span>
        <span className="star-rating__fill" style={{ width: `${percent}%` }}>
          ★★★★★
        </span>
      </span>
      <span className="star-rating__value">{clamped.toFixed(1)}</span>
      {count !== undefined && (
        <span className="star-rating__count">（{count}件）</span>
      )}
    </span>
  );
}

interface StarRatingInputProps {
  value: number;
  onChange: (value: number) => void;
}

/** 投稿フォーム用の操作可能な星評価入力（ラジオボタン群）。 */
export function StarRatingInput({ value, onChange }: StarRatingInputProps) {
  const [hover, setHover] = useState<number | null>(null);
  const active = hover ?? value;

  return (
    <div
      className="star-input"
      role="radiogroup"
      aria-label="星評価（1〜5）"
      onMouseLeave={() => setHover(null)}
    >
      {STAR_VALUES.map((star) => (
        <button
          key={star}
          type="button"
          className={`star-input__star ${star <= active ? 'is-active' : ''}`}
          role="radio"
          aria-checked={value === star}
          aria-label={`星${star}つ`}
          onMouseEnter={() => setHover(star)}
          onFocus={() => setHover(star)}
          onBlur={() => setHover(null)}
          onClick={() => onChange(star)}
        >
          ★
        </button>
      ))}
    </div>
  );
}
