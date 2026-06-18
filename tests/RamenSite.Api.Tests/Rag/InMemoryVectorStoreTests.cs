using FluentAssertions;
using RamenSite.Api.Models;
using RamenSite.Api.Services.Rag;
using Xunit;

namespace RamenSite.Api.Tests.Rag;

/// <summary>
/// SQLite/テスト用フォールバックのベクトルストアの「② Retrieval」を直接検証する。
/// 本番の <c>PgVectorStore</c>（pgvector）は同じ <see cref="IShopVectorStore"/> を実装し、
/// 近傍検索を Postgres 側で行う（実 DB が要るため統合テスト側で担保）。
/// </summary>
public class InMemoryVectorStoreTests
{
    private static Shop MakeShop(int id, string genre, string description) => new()
    {
        Id = id,
        Name = $"店{id}",
        Genre = genre,
        Area = "東京",
        Description = description,
    };

    private static async Task<IShopVectorStore> BuildStoreAsync(params Shop[] shops)
    {
        var store = new InMemoryVectorStore(new ShopEmbeddingIndex(), new SimpleEmbeddingService());
        await store.BuildAsync(shops, CancellationToken.None);
        return store;
    }

    [Fact]
    public async Task SearchAsync_RanksMostSimilarShopFirst()
    {
        // Arrange
        var embedder = new SimpleEmbeddingService();
        var miso = MakeShop(1, "味噌", "コクのある味噌ラーメン");
        var tonkotsu = MakeShop(2, "豚骨", "濃厚な豚骨スープ");
        var store = await BuildStoreAsync(miso, tonkotsu);

        // Act
        var results = await store.SearchAsync(embedder.Embed("味噌ラーメンが食べたい"), 2, CancellationToken.None);

        // Assert
        results.Should().NotBeEmpty();
        results[0].ShopId.Should().Be(miso.Id);
    }

    [Fact]
    public async Task SearchAsync_ReturnsScoresInDescendingOrder()
    {
        var embedder = new SimpleEmbeddingService();
        var store = await BuildStoreAsync(
            MakeShop(1, "味噌", "味噌ラーメン"),
            MakeShop(2, "豚骨", "豚骨ラーメン"),
            MakeShop(3, "塩", "塩ラーメン"));

        var results = await store.SearchAsync(embedder.Embed("豚骨スープ"), 3, CancellationToken.None);

        results.Select(r => r.Score).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task SearchAsync_RespectsTopK()
    {
        var embedder = new SimpleEmbeddingService();
        var store = await BuildStoreAsync(
            MakeShop(1, "味噌", "味噌ラーメン"),
            MakeShop(2, "豚骨", "豚骨ラーメン"),
            MakeShop(3, "塩", "塩ラーメン"));

        var results = await store.SearchAsync(embedder.Embed("ラーメン"), 2, CancellationToken.None);

        results.Count.Should().BeLessThanOrEqualTo(2);
    }
}
