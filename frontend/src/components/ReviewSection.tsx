import { useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useAuth } from '../hooks/useAuth';
import { deleteReview, fetchReviews, submitReview } from '../api/reviews';
import { ApiError } from '../api/client';
import { StarRating, StarRatingInput } from './StarRating';
import type { Review } from '../types';

interface ReviewSectionProps {
  shopId: number;
}

function formatDate(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return '';
  return new Intl.DateTimeFormat('ja-JP', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  }).format(date);
}

/** 店舗詳細のレビュー一覧＋投稿フォーム。 */
export function ReviewSection({ shopId }: ReviewSectionProps) {
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const {
    data: reviews,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ['reviews', shopId],
    queryFn: () => fetchReviews(shopId),
  });

  const myReview = user
    ? reviews?.find((r) => r.userId === user.id)
    : undefined;

  return (
    <section className="detail-section review-section">
      <div className="review-head">
        <h2>レビュー</h2>
        {reviews && reviews.length > 0 && (
          <span className="review-count-label">{reviews.length}件</span>
        )}
      </div>

      {user ? (
        <ReviewForm shopId={shopId} existing={myReview} />
      ) : (
        <p className="review-login-prompt">
          レビューを投稿するには <Link to="/login">ログイン</Link> してください。
        </p>
      )}

      {isLoading && <p className="state-message">読み込み中...</p>}
      {isError && (
        <p className="state-message state-error">
          レビューの取得に失敗しました。
        </p>
      )}
      {reviews && reviews.length === 0 && (
        <p className="review-empty">
          まだレビューがありません。最初の一杯の感想を投稿してみませんか？
        </p>
      )}

      {reviews && reviews.length > 0 && (
        <ul className="review-list">
          {reviews.map((review) => (
            <li key={review.id} className="review-item">
              <div className="review-item__head">
                <span className="review-item__author">{review.displayName}</span>
                <StarRating value={review.rating} size="sm" />
              </div>
              <p className="review-item__comment">{review.comment}</p>
              <div className="review-item__foot">
                <time dateTime={review.updatedAt}>
                  {formatDate(review.updatedAt)}
                </time>
                {user?.id === review.userId && (
                  <DeleteReviewButton
                    shopId={shopId}
                    onDeleted={() => {
                      queryClient.invalidateQueries({
                        queryKey: ['reviews', shopId],
                      });
                      queryClient.invalidateQueries({ queryKey: ['shop', shopId] });
                    }}
                  />
                )}
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

interface ReviewFormProps {
  shopId: number;
  existing?: Review;
}

function ReviewForm({ shopId, existing }: ReviewFormProps) {
  const queryClient = useQueryClient();
  const [rating, setRating] = useState(existing?.rating ?? 0);
  const [comment, setComment] = useState(existing?.comment ?? '');
  const [error, setError] = useState<string | null>(null);

  const mutation = useMutation({
    mutationFn: () => submitReview(shopId, { rating, comment: comment.trim() }),
    onSuccess: () => {
      setError(null);
      queryClient.invalidateQueries({ queryKey: ['reviews', shopId] });
      queryClient.invalidateQueries({ queryKey: ['shop', shopId] });
    },
    onError: (err) => {
      setError(
        err instanceof ApiError ? err.message : 'レビューの投稿に失敗しました。',
      );
    },
  });

  function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (rating < 1) {
      setError('星評価を選択してください。');
      return;
    }
    if (comment.trim().length === 0) {
      setError('コメントを入力してください。');
      return;
    }
    mutation.mutate();
  }

  return (
    <form className="review-form" onSubmit={handleSubmit}>
      <p className="review-form__title">
        {existing ? 'レビューを編集' : 'レビューを書く'}
      </p>
      {error && <p className="form-error">{error}</p>}
      <div className="form-group">
        <label className="form-label">星評価</label>
        <StarRatingInput value={rating} onChange={setRating} />
      </div>
      <div className="form-group">
        <label className="form-label" htmlFor="review-comment">
          コメント
        </label>
        <textarea
          id="review-comment"
          className="form-input review-form__textarea"
          value={comment}
          onChange={(e) => setComment(e.target.value)}
          maxLength={1000}
          rows={4}
          placeholder="スープ・麺・接客など、感じたことを自由に。"
          required
        />
      </div>
      <button
        className="auth-button review-form__submit"
        type="submit"
        disabled={mutation.isPending}
      >
        {mutation.isPending ? '送信中...' : existing ? '更新する' : '投稿する'}
      </button>
    </form>
  );
}

interface DeleteReviewButtonProps {
  shopId: number;
  onDeleted: () => void;
}

function DeleteReviewButton({ shopId, onDeleted }: DeleteReviewButtonProps) {
  const mutation = useMutation({
    mutationFn: () => deleteReview(shopId),
    onSuccess: onDeleted,
  });

  return (
    <button
      className="review-delete-btn"
      type="button"
      onClick={() => mutation.mutate()}
      disabled={mutation.isPending}
    >
      {mutation.isPending ? '削除中...' : '自分のレビューを削除'}
    </button>
  );
}
