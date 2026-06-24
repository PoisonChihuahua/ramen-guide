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

    private async Task<PagedShopResponse> GetShopsAsync(string query = "")
    {
        var response = await _client.GetAsync($"/api/shops{query}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedShopResponse>();
        result.Should().NotBeNull();
        return result!;
    }

    [Fact]
    public async Task GetShops_WithNoFilters_ReturnsAllSeededShops()
    {
        var result = await GetShopsAsync();

        result.Items.Should().NotBeEmpty();
        result.Total.Should().Be(result.Items.Count);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetShops_FilteredByGenre_ReturnsOnlyThatGenre()
    {
        var result = await GetShopsAsync("?genre=味噌");

        result.Items.Should().NotBeEmpty();
        result.Items.Should().OnlyContain(s => s.Genre == "味噌");
        result.Items.Should().Contain(s => s.Name == "札幌味噌堂");
    }

    [Fact]
    public async Task GetShops_FilteredByArea_ReturnsOnlyThatArea()
    {
        var result = await GetShopsAsync("?area=東京");

        result.Items.Should().NotBeEmpty();
        result.Items.Should().OnlyContain(s => s.Area == "東京");
    }

    [Fact]
    public async Task GetShops_WithKeyword_MatchesNameDescriptionOrAddress()
    {
        var result = await GetShopsAsync("?q=札幌");

        result.Items.Should().NotBeEmpty();
        result.Items.Should().OnlyContain(s =>
            s.Name.Contains("札幌") ||
            s.Description.Contains("札幌") ||
            s.Address.Contains("札幌"));
    }

    [Fact]
    public async Task GetShops_WithCombinedFilters_AppliesAndSemantics()
    {
        var result = await GetShopsAsync("?genre=味噌&area=札幌");

        result.Items.Should().NotBeEmpty();
        result.Items.Should().OnlyContain(s => s.Genre == "味噌" && s.Area == "札幌");
    }

    [Fact]
    public async Task GetShops_WithNoMatch_ReturnsEmptyArray()
    {
        var result = await GetShopsAsync("?genre=味噌&area=博多");

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
    }

    [Fact]
    public async Task GetShops_WithLimit_ReturnsAtMostThatManyItems()
    {
        var result = await GetShopsAsync("?limit=2");

        result.Items.Should().HaveCount(2);
        result.Total.Should().BeGreaterThan(2);
        result.Page.Should().Be(1);
        result.Limit.Should().Be(2);
    }

    [Fact]
    public async Task GetShops_SecondPage_ReturnsNextSlice()
    {
        var page1 = await GetShopsAsync("?limit=2&page=1");
        var page2 = await GetShopsAsync("?limit=2&page=2");

        page2.Items.Should().NotBeEmpty();
        page2.Items.Should().NotIntersectWith(page1.Items);
        page2.Total.Should().Be(page1.Total);
        page2.Page.Should().Be(2);
    }

    [Fact]
    public async Task GetShops_PageBeyondTotal_ReturnsEmptyItems()
    {
        var result = await GetShopsAsync("?page=9999");

        result.Items.Should().BeEmpty();
        result.Total.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetShopById_WhenExists_ReturnsThatShop()
    {
        var all = await GetShopsAsync();
        var target = all.Items.First();

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

    [Fact]
    public async Task GetShopOptions_ReturnsDistinctGenresAndAreas()
    {
        var response = await _client.GetAsync("/api/shops/options");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var options = await response.Content.ReadFromJsonAsync<ShopOptionsResponse>();
        options.Should().NotBeNull();
        options!.Genres.Should().NotBeEmpty();
        options.Genres.Should().OnlyHaveUniqueItems();
        options.Areas.Should().NotBeEmpty();
        options.Areas.Should().OnlyHaveUniqueItems();
    }
}

/// <summary>ページ分割された店舗一覧レスポンス。</summary>
public record PagedShopResponse(
    IReadOnlyList<ShopResponse> Items,
    int Total,
    int Page,
    int Limit);

/// <summary>絞り込み候補レスポンス。</summary>
public record ShopOptionsResponse(
    IReadOnlyList<string> Genres,
    IReadOnlyList<string> Areas);

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
