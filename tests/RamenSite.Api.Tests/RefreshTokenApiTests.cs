using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace RamenSite.Api.Tests;

/// <summary>リフレッシュトークンのフロー（refresh / logout / ローテーション）を公開HTTP越しに検証する。</summary>
public class RefreshTokenApiTests : IClassFixture<RamenApiFactory>
{
    private readonly HttpClient _client;

    public RefreshTokenApiTests(RamenApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<AuthResponse> RegisterAsync(string email)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new { email, password = "password123", displayName = "リフレッシュ太郎" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();
        auth!.RefreshToken.Should().NotBeNullOrWhiteSpace();
        return auth;
    }

    [Fact]
    public async Task Register_ReturnsRefreshTokenAlongsideAccessToken()
    {
        var auth = await RegisterAsync("rt-register@example.com");

        auth.Token.Should().NotBeNullOrWhiteSpace();
        auth.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewTokenPair()
    {
        var registered = await RegisterAsync("rt-valid@example.com");

        var response = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            new { refreshToken = registered.RefreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshed = await response.Content.ReadFromJsonAsync<AuthResponse>();
        refreshed.Should().NotBeNull();
        refreshed!.Token.Should().NotBeNullOrWhiteSpace();
        refreshed.RefreshToken.Should().NotBeNullOrWhiteSpace();
        // ローテーションにより新しいリフレッシュトークンが払い出される
        refreshed.RefreshToken.Should().NotBe(registered.RefreshToken);
        refreshed.User.Email.Should().Be("rt-valid@example.com");
    }

    [Fact]
    public async Task Refresh_NewAccessToken_CanAccessProtectedEndpoint()
    {
        var registered = await RegisterAsync("rt-access@example.com");

        var refreshResponse = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            new { refreshToken = registered.RefreshToken });
        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshed!.Token);
        var meResponse = await _client.SendAsync(meRequest);

        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_WithRotatedOldToken_ReturnsUnauthorized()
    {
        var registered = await RegisterAsync("rt-rotate@example.com");

        // 1回目: 成功してローテーションされる
        var first = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            new { refreshToken = registered.RefreshToken });
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2回目: 失効済みの旧トークンは拒否される
        var second = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            new { refreshToken = registered.RefreshToken });

        second.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithUnknownToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            new { refreshToken = "this-token-was-never-issued" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithEmptyToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            new { refreshToken = "" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ThenRefresh_ReturnsUnauthorized()
    {
        var registered = await RegisterAsync("rt-logout@example.com");

        var logout = await _client.PostAsJsonAsync(
            "/api/auth/logout",
            new { refreshToken = registered.RefreshToken });
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var refresh = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            new { refreshToken = registered.RefreshToken });

        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_IsIdempotent_ReturnsNoContentEvenForUnknownToken()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/logout",
            new { refreshToken = "unknown-token" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
