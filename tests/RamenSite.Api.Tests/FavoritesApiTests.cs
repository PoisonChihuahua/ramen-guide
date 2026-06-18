using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace RamenSite.Api.Tests;

/// <summary>お気に入り API（/api/favorites）の振る舞いを検証する。</summary>
public class FavoritesApiTests : IClassFixture<RamenApiFactory>
{
    private readonly HttpClient _client;

    public FavoritesApiTests(RamenApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetFavorites_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/favorites");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddFavorite_ThenListed_ReturnsThatShop()
    {
        var token = await _client.RegisterAndGetTokenAsync("fav1@example.com");

        var add = await _client.SendWithTokenAsync(HttpMethod.Put, "/api/favorites/1", token);
        add.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await _client.SendWithTokenAsync(HttpMethod.Get, "/api/favorites", token);
        var shops = await list.Content.ReadFromJsonAsync<List<ShopResponse>>();
        shops.Should().Contain(s => s.Id == 1);
    }

    [Fact]
    public async Task AddFavorite_IsIdempotent()
    {
        var token = await _client.RegisterAndGetTokenAsync("fav2@example.com");

        await _client.SendWithTokenAsync(HttpMethod.Put, "/api/favorites/2", token);
        var second = await _client.SendWithTokenAsync(HttpMethod.Put, "/api/favorites/2", token);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await _client.SendWithTokenAsync(HttpMethod.Get, "/api/favorites", token);
        var shops = await list.Content.ReadFromJsonAsync<List<ShopResponse>>();
        shops!.Count(s => s.Id == 2).Should().Be(1);
    }

    [Fact]
    public async Task RemoveFavorite_RemovesFromList()
    {
        var token = await _client.RegisterAndGetTokenAsync("fav3@example.com");
        await _client.SendWithTokenAsync(HttpMethod.Put, "/api/favorites/3", token);

        var remove = await _client.SendWithTokenAsync(HttpMethod.Delete, "/api/favorites/3", token);
        remove.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await _client.SendWithTokenAsync(HttpMethod.Get, "/api/favorites", token);
        var shops = await list.Content.ReadFromJsonAsync<List<ShopResponse>>();
        shops.Should().NotContain(s => s.Id == 3);
    }

    [Fact]
    public async Task Status_ReflectsAddAndRemove()
    {
        var token = await _client.RegisterAndGetTokenAsync("fav4@example.com");

        var before = await _client.SendWithTokenAsync(HttpMethod.Get, "/api/favorites/4/status", token);
        var beforeDto = await before.Content.ReadFromJsonAsync<FavoriteStatusResponse>();
        beforeDto!.IsFavorite.Should().BeFalse();

        await _client.SendWithTokenAsync(HttpMethod.Put, "/api/favorites/4", token);

        var after = await _client.SendWithTokenAsync(HttpMethod.Get, "/api/favorites/4/status", token);
        var afterDto = await after.Content.ReadFromJsonAsync<FavoriteStatusResponse>();
        afterDto!.IsFavorite.Should().BeTrue();
    }

    [Fact]
    public async Task AddFavorite_ForMissingShop_ReturnsNotFound()
    {
        var token = await _client.RegisterAndGetTokenAsync("fav5@example.com");

        var response = await _client.SendWithTokenAsync(HttpMethod.Put, "/api/favorites/999999", token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Favorites_AreScopedPerUser()
    {
        var userA = await _client.RegisterAndGetTokenAsync("fav-a@example.com");
        var userB = await _client.RegisterAndGetTokenAsync("fav-b@example.com");

        await _client.SendWithTokenAsync(HttpMethod.Put, "/api/favorites/1", userA);

        var listB = await _client.SendWithTokenAsync(HttpMethod.Get, "/api/favorites", userB);
        var shopsB = await listB.Content.ReadFromJsonAsync<List<ShopResponse>>();
        shopsB.Should().BeEmpty();
    }
}

/// <summary>お気に入り状態レスポンス DTO。</summary>
public record FavoriteStatusResponse(int ShopId, bool IsFavorite);
