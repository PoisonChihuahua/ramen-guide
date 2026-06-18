using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace RamenSite.Api.Tests;

/// <summary>自然文検索 API（POST /api/shops/ask）の振る舞いを公開HTTP越しに検証する。</summary>
public class AskApiTests : IClassFixture<RamenApiFactory>
{
    private readonly HttpClient _client;

    public AskApiTests(RamenApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<AskResponse> AskAsync(string question, int? topK = null)
    {
        var response = await _client.PostAsJsonAsync("/api/shops/ask", new { question, topK });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AskResponse>();
        body.Should().NotBeNull();
        return body!;
    }

    [Fact]
    public async Task Ask_ReturnsMatchesAndAnswer()
    {
        var result = await AskAsync("味噌ラーメンが食べたい");

        result.Matches.Should().NotBeEmpty();
        result.Answer.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Ask_RanksMatchesByDescendingScore()
    {
        var result = await AskAsync("濃厚な豚骨スープ");

        var scores = result.Matches.Select(m => m.Score).ToList();
        scores.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task Ask_ForMisoQuery_RanksMisoShopFirst()
    {
        // 「味噌」を含む質問は、ジャンル・説明に「味噌」を持つ店舗が最上位に来るはず。
        var result = await AskAsync("味噌のラーメンを教えて");

        result.Matches.Should().NotBeEmpty();
        result.Matches[0].Shop.Genre.Should().Be("味噌");
    }

    [Fact]
    public async Task Ask_RespectsTopK()
    {
        var result = await AskAsync("ラーメン", topK: 2);

        result.Matches.Count.Should().BeLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task Ask_WithEmptyQuestion_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/shops/ask", new { question = "  " });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

/// <summary>API レスポンス（自然文検索）。camelCase JSON を System.Text.Json が解釈。</summary>
public record AskResponse(string Question, string Answer, IReadOnlyList<ShopMatch> Matches);

public record ShopMatch(ShopResponse Shop, double Score);
