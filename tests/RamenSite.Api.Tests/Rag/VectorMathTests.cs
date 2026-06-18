using FluentAssertions;
using RamenSite.Api.Services.Rag;
using Xunit;

namespace RamenSite.Api.Tests.Rag;

/// <summary>コサイン類似度の基本的な性質を検証する。</summary>
public class VectorMathTests
{
    [Fact]
    public void Cosine_OfIdenticalVectors_IsOne()
    {
        var v = new[] { 1f, 2f, 3f };

        VectorMath.Cosine(v, v).Should().BeApproximately(1.0, 1e-9);
    }

    [Fact]
    public void Cosine_OfOrthogonalVectors_IsZero()
    {
        var a = new[] { 1f, 0f };
        var b = new[] { 0f, 1f };

        VectorMath.Cosine(a, b).Should().BeApproximately(0.0, 1e-9);
    }

    [Fact]
    public void Cosine_OfOppositeVectors_IsMinusOne()
    {
        var a = new[] { 1f, 1f };
        var b = new[] { -1f, -1f };

        VectorMath.Cosine(a, b).Should().BeApproximately(-1.0, 1e-9);
    }

    [Fact]
    public void Cosine_WithZeroVector_IsZero()
    {
        var a = new[] { 0f, 0f };
        var b = new[] { 1f, 1f };

        VectorMath.Cosine(a, b).Should().Be(0);
    }

    [Fact]
    public void Cosine_WithMismatchedDimensions_Throws()
    {
        var a = new[] { 1f, 2f };
        var b = new[] { 1f, 2f, 3f };

        var act = () => VectorMath.Cosine(a, b);

        act.Should().Throw<ArgumentException>();
    }
}
