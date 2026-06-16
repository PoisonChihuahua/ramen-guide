using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace RamenSite.Api.Tests;

/// <summary>認証フロー（register / login / me / logout）の振る舞いを公開HTTP越しに検証する。</summary>
public class AuthApiTests : IClassFixture<RamenApiFactory>
{
    // CreateClient は既定で Cookie を保持・自動送信する（HandleCookies = true）。
    private readonly HttpClient _client;

    public AuthApiTests(RamenApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private record RegisterBody(string email, string password, string displayName);
    private record LoginBody(string email, string password);

    /// <summary>新規ユーザーを登録してユーザー情報を返すヘルパー。成功時は認証 Cookie がクライアントに保持される。</summary>
    private async Task<AuthUser> RegisterAsync(string email, string password = "password123", string displayName = "テスト太郎")
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterBody(email, password, displayName));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<AuthUser>();
        user.Should().NotBeNull();
        return user!;
    }

    private static bool HasAuthCookie(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var cookies) &&
        cookies.Any(c => c.StartsWith("ramensite_auth=", StringComparison.Ordinal));

    [Fact]
    public async Task Register_WithValidInput_SetsHttpOnlyCookieAndReturnsUser()
    {
        var body = new RegisterBody("tracer@example.com", "password123", "トレーサー太郎");

        var response = await _client.PostAsJsonAsync("/api/auth/register", body);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // トークンは httpOnly Cookie で送られ、レスポンスボディには含まれない
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies!.Should().Contain(c =>
            c.StartsWith("ramensite_auth=", StringComparison.Ordinal) &&
            c.Contains("httponly", StringComparison.OrdinalIgnoreCase));

        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().NotContain("token", "認証トークンはボディに含めず httpOnly Cookie で送る");

        var user = JsonSerializer.Deserialize<AuthUser>(raw,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        user!.Email.Should().Be("tracer@example.com");
        user.DisplayName.Should().Be("トレーサー太郎");
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_SetsHttpOnlyCookie()
    {
        await RegisterAsync("login-ok@example.com", "password123");

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginBody("login-ok@example.com", "password123"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        HasAuthCookie(response).Should().BeTrue();

        var user = await response.Content.ReadFromJsonAsync<AuthUser>();
        user!.Email.Should().Be("login-ok@example.com");
    }

    [Fact]
    public async Task Me_WithAuthCookie_ReturnsCurrentUser()
    {
        // 登録で設定された Cookie はクライアントに保持され、後続リクエストで自動送信される
        await RegisterAsync("me-ok@example.com", displayName: "ミー次郎");

        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<AuthUser>();
        user.Should().NotBeNull();
        user!.Email.Should().Be("me-ok@example.com");
        user.DisplayName.Should().Be("ミー次郎");
    }

    [Fact]
    public async Task Me_WithoutCookie_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ClearsCookie_AndSubsequentMeReturnsUnauthorized()
    {
        await RegisterAsync("logout@example.com");

        // ログアウト前は認証済み
        (await _client.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.OK);

        var logout = await _client.PostAsync("/api/auth/logout", content: null);
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Cookie が破棄され、保護リソースにアクセスできない
        (await _client.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        await RegisterAsync("dup@example.com");

        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterBody("dup@example.com", "password123", "別の人"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        await RegisterAsync("wrongpw@example.com", "password123");

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginBody("wrongpw@example.com", "wrong-password"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginBody("nobody@example.com", "password123"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("short-pw@example.com", "123", "短すぎ")]   // パスワード8文字未満
    [InlineData("not-an-email", "password123", "不正メール")] // メール形式不正
    public async Task Register_WithInvalidInput_ReturnsBadRequest(
        string email, string password, string displayName)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterBody(email, password, displayName));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

/// <summary>認証成功レスポンス（camelCase JSON のユーザー情報）。</summary>
public record AuthUser(int Id, string Email, string DisplayName);
