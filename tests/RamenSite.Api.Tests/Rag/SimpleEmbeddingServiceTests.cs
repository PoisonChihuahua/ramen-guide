using FluentAssertions;
using RamenSite.Api.Services.Rag;
using Xunit;

namespace RamenSite.Api.Tests.Rag;

/// <summary>キー不要の簡易埋め込みが、RAG に必要な性質を満たすか検証する。</summary>
public class SimpleEmbeddingServiceTests
{
    private readonly SimpleEmbeddingService _embedder = new();

    [Fact]
    public void Embed_ReturnsVectorOfFixedDimension()
    {
        var vector = _embedder.Embed("豚骨ラーメン");

        vector.Should().HaveCount(_embedder.Dimension);
    }

    [Fact]
    public void Embed_IsDeterministic_SameTextGivesSameVector()
    {
        // 同じ入力は常に同じベクトルでなければ、索引と質問のベクトルが噛み合わない。
        var a = _embedder.Embed("味噌ラーメン");
        var b = _embedder.Embed("味噌ラーメン");

        a.Should().Equal(b);
    }

    [Fact]
    public void Embed_ProducesL2NormalizedVector()
    {
        var vector = _embedder.Embed("濃厚な豚骨スープ");

        var magnitude = Math.Sqrt(vector.Sum(v => v * (double)v));
        magnitude.Should().BeApproximately(1.0, 1e-6);
    }

    [Fact]
    public void Embed_EmptyText_ReturnsZeroVector()
    {
        var vector = _embedder.Embed("   ");

        vector.Should().OnlyContain(v => v == 0f);
    }

    [Fact]
    public void RelatedTexts_AreMoreSimilarThanUnrelatedTexts()
    {
        // 文字の重なりが多いテキスト同士の方が、無関係なテキストより類似度が高い。
        var query = _embedder.Embed("味噌ラーメン");
        var related = _embedder.Embed("コクのある味噌ラーメン");
        var unrelated = _embedder.Embed("透き通った塩スープ");

        var relatedScore = VectorMath.Cosine(query, related);
        var unrelatedScore = VectorMath.Cosine(query, unrelated);

        relatedScore.Should().BeGreaterThan(unrelatedScore);
    }
}
