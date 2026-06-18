using RamenSite.Api.Models;

namespace RamenSite.Api.Services.Rag;

/// <summary>
/// pgvector を持たない環境（統合テストのインメモリ SQLite）向けのフォールバック実装。
/// 索引は <see cref="ShopEmbeddingIndex"/>（シングルトン）に保持し、検索は全件コサイン計算で行う。
/// 店舗数が小さい本プロジェクトでは十分に機能し、RAG の「② Retrieval」の本質
/// （質問ベクトルと各文書ベクトルの近さで並べる）を最小コードで示す。
/// </summary>
public sealed class InMemoryVectorStore : IShopVectorStore
{
    private readonly ShopEmbeddingIndex _index;
    private readonly IEmbeddingService _embedder;

    public InMemoryVectorStore(ShopEmbeddingIndex index, IEmbeddingService embedder)
    {
        _index = index;
        _embedder = embedder;
    }

    public Task BuildAsync(IReadOnlyList<Shop> shops, CancellationToken cancellationToken)
    {
        _index.Build(shops, _embedder);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ShopScore>> SearchAsync(float[] queryVector, int topK, CancellationToken cancellationToken)
    {
        IReadOnlyList<ShopScore> ranked = _index.Vectors
            .Select(v => new ShopScore(v.ShopId, VectorMath.Cosine(queryVector, v.Vector)))
            .Where(x => x.Score > 0) // 全く重ならない店舗は候補にしない
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .ToList();

        return Task.FromResult(ranked);
    }
}
