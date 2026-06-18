using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;

namespace RamenSite.Api.Tests;

/// <summary>
/// 各統合テストで使う認証ヘルパー。
/// 認証トークンは httpOnly Cookie で発行されるため、Set-Cookie ヘッダから JWT を取り出し、
/// 後続リクエストには Authorization: Bearer で付与する（サーバーはヘッダ方式も受け付ける）。
/// </summary>
internal static class AuthTestHelpers
{
    private const string AuthCookieName = "ramensite_auth";

    private record RegisterBody(string email, string password, string displayName);
    private record LoginBody(string email, string password);

    /// <summary>シードされた既定の管理者アカウント（SeedData と一致）。</summary>
    public const string AdminEmail = "admin@ramen.test";
    public const string AdminPassword = "adminpass123";

    /// <summary>新規ユーザーを登録し、発行されたトークンを返す。</summary>
    public static async Task<string> RegisterAndGetTokenAsync(
        this HttpClient client,
        string email,
        string password = "password123",
        string displayName = "テスト太郎")
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/register", new RegisterBody(email, password, displayName));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return ExtractToken(response);
    }

    /// <summary>シード済みの管理者でログインしてトークンを返す。</summary>
    public static async Task<string> LoginAsAdminAsync(this HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login", new LoginBody(AdminEmail, AdminPassword));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return ExtractToken(response);
    }

    /// <summary>Bearer トークンを付けたリクエストを送る。</summary>
    public static Task<HttpResponseMessage> SendWithTokenAsync(
        this HttpClient client,
        HttpMethod method,
        string url,
        string token,
        object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }
        return client.SendAsync(request);
    }

    /// <summary>Set-Cookie ヘッダから認証トークンを取り出す。</summary>
    private static string ExtractToken(HttpResponseMessage response)
    {
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        var cookie = cookies!.First(c =>
            c.StartsWith($"{AuthCookieName}=", StringComparison.Ordinal));

        var start = AuthCookieName.Length + 1;
        var end = cookie.IndexOf(';', start);
        var token = end < 0 ? cookie[start..] : cookie[start..end];
        token.Should().NotBeNullOrWhiteSpace();
        return token;
    }
}
