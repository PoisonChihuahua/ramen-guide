using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace RamenSite.Api.Tests;

/// <summary>レビュー API（GET/POST/DELETE /api/shops/{id}/reviews）の振る舞いを検証する。</summary>
public class ReviewsApiTests : IClassFixture<RamenApiFactory>
{
    private readonly HttpClient _client;

    public ReviewsApiTests(RamenApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private record ReviewBody(int rating, string comment);

    [Fact]
    public async Task GetReviews_ForExistingShop_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/shops/1/reviews");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reviews = await response.Content.ReadFromJsonAsync<List<ReviewResponse>>();
        reviews.Should().NotBeNull();
    }

    [Fact]
    public async Task GetReviews_ForMissingShop_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/shops/999999/reviews");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostReview_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/shops/2/reviews", new ReviewBody(5, "おいしい"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostReview_WithValidInput_CreatesAndAppearsInList()
    {
        var token = await _client.RegisterAndGetTokenAsync("reviewer1@example.com", displayName: "レビュー太郎");

        var create = await _client.SendWithTokenAsync(
            HttpMethod.Post, "/api/shops/2/reviews", token, new ReviewBody(4, "コクがあって好み"));

        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.Content.ReadFromJsonAsync<ReviewResponse>();
        created.Should().NotBeNull();
        created!.Rating.Should().Be(4);
        created.Comment.Should().Be("コクがあって好み");
        created.DisplayName.Should().Be("レビュー太郎");

        var list = await _client.GetFromJsonAsync<List<ReviewResponse>>("/api/shops/2/reviews");
        list.Should().Contain(r => r.Id == created.Id);
    }

    [Fact]
    public async Task PostReview_Twice_UpdatesExistingInsteadOfDuplicating()
    {
        var token = await _client.RegisterAndGetTokenAsync("reviewer2@example.com");

        await _client.SendWithTokenAsync(
            HttpMethod.Post, "/api/shops/3/reviews", token, new ReviewBody(3, "普通"));
        var second = await _client.SendWithTokenAsync(
            HttpMethod.Post, "/api/shops/3/reviews", token, new ReviewBody(5, "やっぱり最高"));

        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await second.Content.ReadFromJsonAsync<ReviewResponse>();
        updated!.Rating.Should().Be(5);
        updated.Comment.Should().Be("やっぱり最高");

        var list = await _client.GetFromJsonAsync<List<ReviewResponse>>("/api/shops/3/reviews");
        list!.Count(r => r.UserId == updated.UserId).Should().Be(1);
    }

    [Theory]
    [InlineData(0, "範囲外（低）")]
    [InlineData(6, "範囲外（高）")]
    public async Task PostReview_WithInvalidRating_ReturnsBadRequest(int rating, string comment)
    {
        var token = await _client.RegisterAndGetTokenAsync($"reviewer-bad-{rating}@example.com");

        var response = await _client.SendWithTokenAsync(
            HttpMethod.Post, "/api/shops/4/reviews", token, new ReviewBody(rating, comment));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteReview_RemovesOwnReview()
    {
        var token = await _client.RegisterAndGetTokenAsync("reviewer3@example.com");
        await _client.SendWithTokenAsync(
            HttpMethod.Post, "/api/shops/5/reviews", token, new ReviewBody(4, "また来たい"));

        var delete = await _client.SendWithTokenAsync(HttpMethod.Delete, "/api/shops/5/reviews", token);
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var list = await _client.GetFromJsonAsync<List<ReviewResponse>>("/api/shops/5/reviews");
        list!.Should().NotContain(r => r.UserId == 0);
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task PostReview_UpdatesShopAverageRating()
    {
        var token = await _client.RegisterAndGetTokenAsync("reviewer4@example.com");

        await _client.SendWithTokenAsync(
            HttpMethod.Post, "/api/shops/6/reviews", token, new ReviewBody(5, "星5つ"));

        var shop = await _client.GetFromJsonAsync<ShopResponse>("/api/shops/6");
        shop!.ReviewCount.Should().BeGreaterThanOrEqualTo(1);
        shop.AverageRating.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DeleteReview_WithoutExistingReview_ReturnsNotFound()
    {
        var token = await _client.RegisterAndGetTokenAsync("reviewer5@example.com");

        // レビューを投稿せずに削除を試みる
        var delete = await _client.SendWithTokenAsync(HttpMethod.Delete, "/api/shops/1/reviews", token);

        delete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteReview_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.DeleteAsync("/api/shops/1/reviews");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

/// <summary>レビュー API レスポンス DTO。</summary>
public record ReviewResponse(
    int Id,
    int ShopId,
    int UserId,
    string DisplayName,
    int Rating,
    string Comment,
    DateTime CreatedAt,
    DateTime UpdatedAt);
