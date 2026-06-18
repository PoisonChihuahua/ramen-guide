using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace RamenSite.Api.Tests;

/// <summary>店舗検索・絞り込み API（GET /api/shops）の振る舞いを公開HTTP越しに検証する。</summary>
public class ShopsApiTests : IClassFixture<RamenApiFactory>
{
    private readonly HttpClient _client;

    public ShopsApiTests(RamenApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<IReadOnlyList<ShopResponse>> GetShopsAsync(string query = "")
    {
        var response = await _client.GetAsync($"/api/shops{query}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var shops = await response.Content.ReadFromJsonAsync<List<ShopResponse>>();
        shops.Should().NotBeNull();
        return shops!;
    }

    [Fact]
    public async Task GetShops_WithNoFilters_ReturnsAllSeededShops()
    {
        var shops = await GetShopsAsync();

        shops.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetShops_FilteredByGenre_ReturnsOnlyThatGenre()
    {
        var shops = await GetShopsAsync("?genre=味噌");

        shops.Should().NotBeEmpty();
        shops.Should().OnlyContain(s => s.Genre == "味噌");
        shops.Should().Contain(s => s.Name == "札幌味噌堂");
    }

    [Fact]
    public async Task GetShops_FilteredByArea_ReturnsOnlyThatArea()
    {
        var shops = await GetShopsAsync("?area=東京");

        shops.Should().NotBeEmpty();
        shops.Should().OnlyContain(s => s.Area == "東京");
    }

    [Fact]
    public async Task GetShops_WithKeyword_MatchesNameDescriptionOrAddress()
    {
        var shops = await GetShopsAsync("?q=札幌");

        shops.Should().NotBeEmpty();
        shops.Should().OnlyContain(s =>
            s.Name.Contains("札幌") ||
            s.Description.Contains("札幌") ||
            s.Address.Contains("札幌"));
    }

    [Fact]
    public async Task GetShops_WithCombinedFilters_AppliesAndSemantics()
    {
        var shops = await GetShopsAsync("?genre=味噌&area=札幌");

        shops.Should().NotBeEmpty();
        shops.Should().OnlyContain(s => s.Genre == "味噌" && s.Area == "札幌");
    }

    [Fact]
    public async Task GetShops_WithNoMatch_ReturnsEmptyArray()
    {
        var shops = await GetShopsAsync("?genre=味噌&area=博多");

        shops.Should().BeEmpty();
    }

    [Fact]
    public async Task GetShopById_WhenExists_ReturnsThatShop()
    {
        var all = await GetShopsAsync();
        var target = all.First();

        var response = await _client.GetAsync($"/api/shops/{target.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var shop = await response.Content.ReadFromJsonAsync<ShopResponse>();
        shop.Should().NotBeNull();
        shop!.Id.Should().Be(target.Id);
        shop.Name.Should().Be(target.Name);
    }

    [Fact]
    public async Task GetShopById_WhenMissing_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/shops/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

/// <summary>API レスポンスの店舗 DTO（camelCase JSON を System.Text.Json が解釈）。</summary>
public record ShopResponse(
    int Id,
    string Name,
    string Description,
    string Address,
    string Area,
    string Genre,
    string OpeningHours,
    string PriceRange,
    string ImageUrl,
    double AverageRating,
    int ReviewCount);
