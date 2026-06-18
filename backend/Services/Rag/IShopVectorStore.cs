using RamenSite.Api.Models;

namespace RamenSite.Api.Services.Rag;

/// <summary>店舗1件の検索結果（店舗IDと質問との類似度スコア）。</summary>
public readonly record struct ShopScore(int ShopId, double Score);

/// <summary>
/// RAG の「① Indexing」と「② Retrieval」を担うベクトルストアの抽象。
///
/// 学習ポイント: ここを差し替えると「索引と検索をどこで行うか」を切り替えられる。
///   - <see cref="PgVectorStore"/>       … 本番（Postgres + pgvector）。ベクトルをDBに保存し、SQL の <c>&lt;=&gt;</c> 演算子で近傍検索する。
///   - <see cref="InMemoryVectorStore"/> … テスト/SQLite 用フォールバック。メモリ上の全件コサイン計算。
/// 実運用のベクトル検索は <see cref="PgVectorStore"/> のように DB 側で行うのが本筋。
/// </summary>
public interface IShopVectorStore
{
    /// <summary>全店舗をベクトル化して索引を（再）構築する。起動時に一度呼ぶ。</summary>
    Task BuildAsync(IReadOnlyList<Shop> shops, CancellationToken cancellationToken);

    /// <summary>質問ベクトルに近い店舗を上位 <paramref name="topK"/> 件、類似度の高い順に返す。</summary>
    Task<IReadOnlyList<ShopScore>> SearchAsync(float[] queryVector, int topK, CancellationToken cancellationToken);
}
