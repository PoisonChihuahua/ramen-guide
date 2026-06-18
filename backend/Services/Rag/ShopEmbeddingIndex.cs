using RamenSite.Api.Models;

namespace RamenSite.Api.Services.Rag;

/// <summary>店舗1件分の埋め込みベクトル。</summary>
public readonly record struct ShopVector(int ShopId, float[] Vector);

/// <summary>
/// 全店舗の埋め込みベクトルをメモリ上に保持する索引（RAG の「① Indexing」の成果物）。
///
/// 学習ポイント: 本来 RAG ではここをベクトルDB（pgvector, sqlite-vec 等）に置く。
/// このプロジェクトは店舗数が小さいので、あえて素朴な List + 全件コサイン計算にしている。
/// 「索引とは事前にベクトル化して持っておくこと」という本質が見えるのが狙い。
/// シングルトンで登録し、起動時に一度だけ構築する。
/// </summary>
public sealed class ShopEmbeddingIndex
{
    private readonly List<ShopVector> _vectors = new();

    public IReadOnlyList<ShopVector> Vectors => _vectors;

    /// <summary>埋め込み対象にする店舗のテキスト（名前・ジャンル・エリア・説明をまとめる）。</summary>
    public static string DocumentText(Shop shop) =>
        $"{shop.Name} {shop.Genre} {shop.Area} {shop.Description}";

    /// <summary>店舗群を埋め込んで索引を作り直す。</summary>
    public void Build(IEnumerable<Shop> shops, IEmbeddingService embedder)
    {
        _vectors.Clear();
        foreach (var shop in shops)
        {
            _vectors.Add(new ShopVector(shop.Id, embedder.Embed(DocumentText(shop))));
        }
    }
}
