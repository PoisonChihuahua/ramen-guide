using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace RamenSite.Api.Tests;

/// <summary>認証フロー（register / login / me）の振る舞いを公開HTTP越しに検証する。</summary>
public class AuthApiTests : IClassFixture<RamenApiFactory>
{
    private readonly HttpClient _client;

    public AuthApiTests(RamenApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private record RegisterBody(string email, string password, string displayName);
    private record LoginBody(string email, string password);

    /// <summary>新規ユーザーを登録し、認証レスポンスを返すヘルパー。</summary>
    private async Task<AuthResponse> RegisterAsync(string email, string password = "password123", string displayName = "テスト太郎")
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterBody(email, password, displayName));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();
        return auth!;
    }

    [Fact]
    public async Task Register_WithValidInput_ReturnsTokenAndUser()
    {
        var body = new RegisterBody("tracer@example.com", "password123", "トレーサー太郎");

        var response = await _client.PostAsJsonAsync("/api/auth/register", body);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();
        auth!.Token.Should().NotBeNullOrWhiteSpace();
        auth.User.Email.Should().Be("tracer@example.com");
        auth.User.DisplayName.Should().Be("トレーサー太郎");
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsToken()
    {
        await RegisterAsync("login-ok@example.com", "password123");

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginBody("login-ok@example.com", "password123"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();
        auth!.Token.Should().NotBeNullOrWhiteSpace();
        auth.User.Email.Should().Be("login-ok@example.com");
    }

    [Fact]
    public async Task Me_WithValidToken_ReturnsCurrentUser()
    {
        var registered = await RegisterAsync("me-ok@example.com", displayName: "ミー次郎");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", registered.Token);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<AuthUser>();
        user.Should().NotBeNull();
        user!.Email.Should().Be("me-ok@example.com");
        user.DisplayName.Should().Be("ミー次郎");
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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

/// <summary>認証成功レスポンス（camelCase JSON）。</summary>
public record AuthResponse(string Token, AuthUser User);

public record AuthUser(int Id, string Email, string DisplayName);
