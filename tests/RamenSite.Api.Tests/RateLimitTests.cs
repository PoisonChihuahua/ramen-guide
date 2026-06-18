using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace RamenSite.Api.Tests;

/// <summary>認証エンドポイントのレート制限（総当たり抑止）を検証する。</summary>
public class RateLimitTests : IClassFixture<RamenApiFactory>
{
    private readonly RamenApiFactory _factory;

    public RateLimitTests(RamenApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WhenExceedingAuthRateLimit_Returns429()
    {
        // このテスト専用に上限を低く差し替えた独立ホストを起動する。
        var client = _factory
            .WithWebHostBuilder(builder =>
                builder.UseSetting("RateLimiting:AuthPermitLimit", "3"))
            .CreateClient();

        var body = new { email = "ratelimit@example.com", password = "wrong-password" };

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 5; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", body);
            statuses.Add(response.StatusCode);
        }

        // 上限(3)までは認証失敗(401)、それ以降は 429 になる。
        statuses.Take(3).Should().AllBeEquivalentTo(HttpStatusCode.Unauthorized);
        statuses.Should().Contain(HttpStatusCode.TooManyRequests);
    }
}
