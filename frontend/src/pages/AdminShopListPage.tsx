import { Link } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { deleteShop, fetchShops } from '../api/shops';
import type { Shop } from '../types';

/** 管理者向けの店舗一覧（編集・削除・新規追加への入口）。 */
export function AdminShopListPage() {
  const {
    data: shops,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ['shops', {}],
    queryFn: () => fetchShops(),
  });

  return (
    <main className="admin-page">
      <div className="admin-head">
        <div>
          <h1 className="section-title">店舗管理</h1>
          <p className="section-sub">Admin · Shops</p>
        </div>
        <Link className="auth-button admin-new-btn" to="/admin/shops/new">
          ＋ 新規追加
        </Link>
      </div>

      {isLoading && <p className="state-message">読み込み中...</p>}
      {isError && (
        <p className="state-message state-error">
          店舗情報の取得に失敗しました。
        </p>
      )}

      {shops && shops.length > 0 && (
        <table className="admin-table">
          <thead>
            <tr>
              <th>ID</th>
              <th>店名</th>
              <th>エリア</th>
              <th>ジャンル</th>
              <th>評価</th>
              <th className="admin-table__actions">操作</th>
            </tr>
          </thead>
          <tbody>
            {shops.map((shop) => (
              <AdminShopRow key={shop.id} shop={shop} />
            ))}
          </tbody>
        </table>
      )}
    </main>
  );
}

function AdminShopRow({ shop }: { shop: Shop }) {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: () => deleteShop(shop.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['shops'] });
    },
  });

  function handleDelete() {
    if (window.confirm(`「${shop.name}」を削除しますか？この操作は取り消せません。`)) {
      mutation.mutate();
    }
  }

  return (
    <tr>
      <td>{shop.id}</td>
      <td>{shop.name}</td>
      <td>{shop.area}</td>
      <td>{shop.genre}</td>
      <td>
        {shop.reviewCount > 0
          ? `★${shop.averageRating.toFixed(1)} (${shop.reviewCount})`
          : '—'}
      </td>
      <td className="admin-table__actions">
        <Link className="admin-action-link" to={`/admin/shops/${shop.id}/edit`}>
          編集
        </Link>
        <button
          className="admin-action-link admin-action-link--danger"
          type="button"
          onClick={handleDelete}
          disabled={mutation.isPending}
        >
          {mutation.isPending ? '削除中...' : '削除'}
        </button>
      </td>
    </tr>
  );
}
