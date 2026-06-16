using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace RamenSite.Api.Tests;

/// <summary>店舗 CRUD（管理者専用）の振る舞いと権限制御を検証する。</summary>
public class AdminShopsApiTests : IClassFixture<RamenApiFactory>
{
    private readonly HttpClient _client;

    public AdminShopsApiTests(RamenApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private record ShopBody(
        string name, string description, string address, string area,
        string genre, string openingHours, string priceRange, string imageUrl);

    private static ShopBody ValidShop(string name = "新店 テスト") => new(
        name, "テスト用の説明文。", "東京都新宿区1-1-1", "東京",
        "醤油", "11:00〜22:00", "〜1000円", "https://example.com/ramen.jpg");

    [Fact]
    public async Task CreateShop_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/shops", ValidShop());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateShop_AsNormalUser_ReturnsForbidden()
    {
        var token = await _client.RegisterAndGetTokenAsync("plainuser@example.com");

        var response = await _client.SendWithTokenAsync(
            HttpMethod.Post, "/api/shops", token, ValidShop());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateShop_AsAdmin_CreatesShop()
    {
        var token = await _client.LoginAsAdminAsync();

        var response = await _client.SendWithTokenAsync(
            HttpMethod.Post, "/api/shops", token, ValidShop("管理者が作った店"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<ShopResponse>();
        created.Should().NotBeNull();
        created!.Name.Should().Be("管理者が作った店");
        created.Id.Should().BeGreaterThan(0);

        // 一覧から取得できる
        var fetched = await _client.GetFromJsonAsync<ShopResponse>($"/api/shops/{created.Id}");
        fetched!.Name.Should().Be("管理者が作った店");
    }

    [Fact]
    public async Task CreateShop_WithInvalidInput_ReturnsBadRequest()
    {
        var token = await _client.LoginAsAdminAsync();
        var invalid = ValidShop() with { name = "", imageUrl = "not-a-url" };

        var response = await _client.SendWithTokenAsync(
            HttpMethod.Post, "/api/shops", token, invalid);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateShop_AsAdmin_ChangesFields()
    {
        var token = await _client.LoginAsAdminAsync();
        var create = await _client.SendWithTokenAsync(
            HttpMethod.Post, "/api/shops", token, ValidShop("更新前の店"));
        var created = await create.Content.ReadFromJsonAsync<ShopResponse>();

        var updated = ValidShop("更新後の店") with { genre = "味噌" };
        var response = await _client.SendWithTokenAsync(
            HttpMethod.Put, $"/api/shops/{created!.Id}", token, updated);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<ShopResponse>();
        dto!.Name.Should().Be("更新後の店");
        dto.Genre.Should().Be("味噌");
    }

    [Fact]
    public async Task UpdateShop_WhenMissing_ReturnsNotFound()
    {
        var token = await _client.LoginAsAdminAsync();

        var response = await _client.SendWithTokenAsync(
            HttpMethod.Put, "/api/shops/999999", token, ValidShop());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteShop_AsAdmin_RemovesShop()
    {
        var token = await _client.LoginAsAdminAsync();
        var create = await _client.SendWithTokenAsync(
            HttpMethod.Post, "/api/shops", token, ValidShop("削除される店"));
        var created = await create.Content.ReadFromJsonAsync<ShopResponse>();

        var response = await _client.SendWithTokenAsync(
            HttpMethod.Delete, $"/api/shops/{created!.Id}", token);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var fetch = await _client.GetAsync($"/api/shops/{created.Id}");
        fetch.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteShop_AsNormalUser_ReturnsForbidden()
    {
        var token = await _client.RegisterAndGetTokenAsync("plainuser2@example.com");

        var response = await _client.SendWithTokenAsync(
            HttpMethod.Delete, "/api/shops/1", token);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteShop_WhenMissing_ReturnsNotFound()
    {
        var token = await _client.LoginAsAdminAsync();

        var response = await _client.SendWithTokenAsync(
            HttpMethod.Delete, "/api/shops/999999", token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
