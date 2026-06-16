import { useState, type FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { createShop, fetchShop, updateShop } from '../api/shops';
import { ApiError } from '../api/client';
import type { Shop, ShopInput } from '../types';

const EMPTY_FORM: ShopInput = {
  name: '',
  description: '',
  address: '',
  area: '',
  genre: '',
  openingHours: '',
  priceRange: '',
  imageUrl: '',
};

const FIELDS: { key: keyof ShopInput; label: string; multiline?: boolean }[] = [
  { key: 'name', label: '店名' },
  { key: 'description', label: '説明', multiline: true },
  { key: 'address', label: '住所' },
  { key: 'area', label: 'エリア' },
  { key: 'genre', label: 'ジャンル' },
  { key: 'openingHours', label: '営業時間' },
  { key: 'priceRange', label: '価格帯' },
  { key: 'imageUrl', label: '画像URL' },
];

function toInput(shop: Shop): ShopInput {
  return {
    name: shop.name,
    description: shop.description,
    address: shop.address,
    area: shop.area,
    genre: shop.genre,
    openingHours: shop.openingHours,
    priceRange: shop.priceRange,
    imageUrl: shop.imageUrl,
  };
}

/** 店舗の新規作成・編集フォーム（管理者専用）。 */
export function AdminShopFormPage() {
  const { id } = useParams<{ id: string }>();
  const isEdit = id !== undefined;
  const shopId = Number(id);

  const { data: existing, isLoading } = useQuery({
    queryKey: ['shop', shopId],
    queryFn: () => fetchShop(shopId),
    enabled: isEdit && Number.isFinite(shopId),
  });

  return (
    <main className="admin-page admin-form-page">
      <nav className="detail-breadcrumb" aria-label="パンくず">
        <Link to="/admin">店舗管理</Link>
        <span className="sep">›</span>
        <span aria-current="page">{isEdit ? '編集' : '新規追加'}</span>
      </nav>

      <h1 className="section-title">
        {isEdit ? '店舗を編集' : '店舗を新規追加'}
      </h1>

      {isEdit && isLoading ? (
        <p className="state-message">読み込み中...</p>
      ) : (
        // 編集時はデータ取得後にマウントし、初期値を確定させる
        <ShopForm
          shopId={isEdit ? shopId : undefined}
          initial={existing ? toInput(existing) : EMPTY_FORM}
        />
      )}
    </main>
  );
}

interface ShopFormProps {
  /** 既存店舗ID。undefined のとき新規作成。 */
  shopId?: number;
  initial: ShopInput;
}

function ShopForm({ shopId, initial }: ShopFormProps) {
  const isEdit = shopId !== undefined;
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [form, setForm] = useState<ShopInput>(initial);
  const [error, setError] = useState<string | null>(null);

  const mutation = useMutation({
    mutationFn: () =>
      isEdit ? updateShop(shopId, form) : createShop(form),
    onSuccess: (shop) => {
      queryClient.invalidateQueries({ queryKey: ['shops'] });
      queryClient.invalidateQueries({ queryKey: ['shop', shop.id] });
      navigate('/admin');
    },
    onError: (err) => {
      setError(err instanceof ApiError ? err.message : '保存に失敗しました。');
    },
  });

  function handleChange(key: keyof ShopInput, value: string) {
    setForm((prev) => ({ ...prev, [key]: value }));
  }

  function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    mutation.mutate();
  }

  return (
    <form className="admin-form" onSubmit={handleSubmit}>
      {error && <p className="form-error">{error}</p>}
      {FIELDS.map((field) => (
        <div className="form-group" key={field.key}>
          <label className="form-label" htmlFor={`shop-${field.key}`}>
            {field.label}
          </label>
          {field.multiline ? (
            <textarea
              id={`shop-${field.key}`}
              className="form-input review-form__textarea"
              value={form[field.key]}
              onChange={(e) => handleChange(field.key, e.target.value)}
              rows={4}
              required
            />
          ) : (
            <input
              id={`shop-${field.key}`}
              className="form-input"
              type={field.key === 'imageUrl' ? 'url' : 'text'}
              value={form[field.key]}
              onChange={(e) => handleChange(field.key, e.target.value)}
              required
            />
          )}
        </div>
      ))}
      <div className="admin-form__actions">
        <button className="auth-button" type="submit" disabled={mutation.isPending}>
          {mutation.isPending ? '保存中...' : isEdit ? '更新する' : '追加する'}
        </button>
        <Link className="info-btn info-btn--ghost" to="/admin">
          キャンセル
        </Link>
      </div>
    </form>
  );
}
