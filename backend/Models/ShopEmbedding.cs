using Pgvector;

namespace RamenSite.Api.Models;

/// <summary>
/// 店舗1件の埋め込みベクトル（pgvector）。RAG の索引（① Indexing）の実体で、
/// <c>ShopEmbeddings</c> テーブルに <c>vector(512)</c> 列として保存する。
///
/// このエンティティは Postgres 専用。<see cref="Data.AppDbContext.OnModelCreating"/> で
/// Npgsql のときだけモデルに登録するため、統合テストの SQLite には作られない
/// （SQLite に <c>vector</c> 型は無い）。
/// </summary>
public class ShopEmbedding
{
    /// <summary>対応する店舗ID（主キー兼 <see cref="Shop"/> への外部キー）。</summary>
    public int ShopId { get; set; }

    /// <summary>埋め込みベクトル。次元は <see cref="Services.Rag.SimpleEmbeddingService"/> の出力に一致させる。</summary>
    public Vector Embedding { get; set; } = null!;
}
