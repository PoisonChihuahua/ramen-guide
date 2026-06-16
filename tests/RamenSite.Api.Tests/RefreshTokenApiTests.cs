using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace RamenSite.Api.Tests;

/// <summary>
/// リフレッシュトークン（httpOnly Cookie）のフロー — refresh / logout / ローテーション — を
/// 公開HTTP越しに検証する。Cookie を手動制御するため自動 Cookie 処理は無効にしている。
/// </summary>
public class RefreshTokenApiTests : IClassFixture<RamenApiFactory>
{
    private const string AuthCookieName = "ramensite_auth";
    private const string RefreshCookieName = "ramensite_refresh";

    private readonly HttpClient _client;

    public RefreshTokenApiTests(RamenApiFactory factory)
    {
        // Cookie の差し替え・失効を厳密に検証するため、クライアント側の自動 Cookie 保持を無効化する。
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
        });
    }

    private record RegisterBody(string email, string password, string displayName);

    /// <summary>Set-Cookie ヘッダから指定 Cookie の値を取り出す。</summary>
    private static string? ExtractCookie(HttpResponseMessage response, string name)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return null;
        }

        var prefix = name + "=";
        var header = cookies.FirstOrDefault(c => c.StartsWith(prefix, StringComparison.Ordinal));
        if (header is null)
        {
            return null;
        }

        var value = header[prefix.Length..];
        var semicolon = value.IndexOf(';');
        return semicolon >= 0 ? value[..semicolon] : value;
    }

    /// <summary>新規登録し、発行されたアクセス／リフレッシュ Cookie の値を返す。</summary>
    private async Task<(string Access, string Refresh)> RegisterAsync(string email)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterBody(email, "password123", "リフレッシュ太郎"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var access = ExtractCookie(response, AuthCookieName);
        var refresh = ExtractCookie(response, RefreshCookieName);
        access.Should().NotBeNullOrWhiteSpace();
        refresh.Should().NotBeNullOrWhiteSpace();
        return (access!, refresh!);
    }

    private HttpRequestMessage Post(string path, params string[] cookies)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        if (cookies.Length > 0)
        {
            request.Headers.Add("Cookie", string.Join("; ", cookies));
        }
        return request;
    }

    [Fact]
    public async Task Register_SetsBothAuthAndRefreshCookies()
    {
        var (access, refresh) = await RegisterAsync("rt-register@example.com");

        access.Should().NotBeNullOrWhiteSpace();
        refresh.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Refresh_WithValidCookie_IssuesNewCookiePair()
    {
        var (_, refresh) = await RegisterAsync("rt-valid@example.com");

        var response = await _client.SendAsync(
            Post("/api/auth/refresh", $"{RefreshCookieName}={refresh}"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ExtractCookie(response, AuthCookieName).Should().NotBeNullOrWhiteSpace();
        var rotated = ExtractCookie(response, RefreshCookieName);
        rotated.Should().NotBeNullOrWhiteSpace();
        // ローテーションにより新しいリフレッシュトークンが払い出される
        rotated.Should().NotBe(refresh);
    }

    [Fact]
    public async Task Refresh_NewAccessCookie_CanAccessProtectedEndpoint()
    {
        var (_, refresh) = await RegisterAsync("rt-access@example.com");

        var refreshResponse = await _client.SendAsync(
            Post("/api/auth/refresh", $"{RefreshCookieName}={refresh}"));
        var newAccess = ExtractCookie(refreshResponse, AuthCookieName);

        var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        meRequest.Headers.Add("Cookie", $"{AuthCookieName}={newAccess}");
        var meResponse = await _client.SendAsync(meRequest);

        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_WithRotatedOldCookie_ReturnsUnauthorized()
    {
        var (_, refresh) = await RegisterAsync("rt-rotate@example.com");

        // 1回目: 成功してローテーションされる
        var first = await _client.SendAsync(
            Post("/api/auth/refresh", $"{RefreshCookieName}={refresh}"));
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2回目: 失効済みの旧トークンは拒否される
        var second = await _client.SendAsync(
            Post("/api/auth/refresh", $"{RefreshCookieName}={refresh}"));

        second.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithoutCookie_ReturnsUnauthorized()
    {
        var response = await _client.SendAsync(Post("/api/auth/refresh"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithUnknownCookie_ReturnsUnauthorized()
    {
        var response = await _client.SendAsync(
            Post("/api/auth/refresh", $"{RefreshCookieName}=this-was-never-issued"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ThenRefresh_ReturnsUnauthorized()
    {
        var (_, refresh) = await RegisterAsync("rt-logout@example.com");

        var logout = await _client.SendAsync(
            Post("/api/auth/logout", $"{RefreshCookieName}={refresh}"));
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var refreshAfter = await _client.SendAsync(
            Post("/api/auth/refresh", $"{RefreshCookieName}={refresh}"));

        refreshAfter.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_IsIdempotent_ReturnsNoContentWithoutCookie()
    {
        var response = await _client.SendAsync(Post("/api/auth/logout"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
