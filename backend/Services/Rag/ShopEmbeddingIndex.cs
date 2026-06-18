using RamenSite.Api.Models;

namespace RamenSite.Api.Services.Rag;

/// <summary>店舗1件分の埋め込みベクトル。</summary>
public readonly record struct ShopVector(int ShopId, float[] Vector);

/// <summary>
/// 全店舗の埋め込みベクトルを「メモリ上」に保持する索引。
/// <see cref="InMemoryVectorStore"/> のバッキングストアで、シングルトンで登録し
/// 起動時に一度だけ構築する。
///
/// 学習ポイント: 本番は <see cref="PgVectorStore"/> が pgvector（Postgres）に索引を持つ。
/// こちらは pgvector を持たない SQLite（統合テスト）向けのフォールバックで、
/// 「索引とは事前にベクトル化して持っておくこと」という本質を最小実装で見せる。
/// </summary>
public sealed class ShopEmbeddingIndex
{
    private readonly List<ShopVector> _vectors = new();

    public IReadOnlyList<ShopVector> Vectors => _vectors;

    /// <summary>店舗群を埋め込んで索引を作り直す。</summary>
    public void Build(IEnumerable<Shop> shops, IEmbeddingService embedder)
    {
        _vectors.Clear();
        foreach (var shop in shops)
        {
            _vectors.Add(new ShopVector(shop.Id, embedder.Embed(ShopDocument.Text(shop))));
        }
    }
}
