using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RamenSite.Api.Data;
using RamenSite.Api.Dtos;
using RamenSite.Api.Models;
using RamenSite.Api.Services;

namespace RamenSite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting(RateLimitPolicies.Auth)]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokenService;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly JwtSettings _jwtSettings;

    public AuthController(
        AppDbContext db,
        TokenService tokenService,
        IPasswordHasher<User> passwordHasher,
        IOptions<JwtSettings> jwtSettings)
    {
        _db = db;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _jwtSettings = jwtSettings.Value;
    }

    /// <summary>ユーザー登録。成功で httpOnly Cookie にアクセス／リフレッシュトークンを設定する。</summary>
    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email))
        {
            return Conflict(new { message = "このメールアドレスは既に登録されています。" });
        }

        var user = new User
        {
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            CreatedAt = DateTime.UtcNow,
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(await IssueAuthCookiesAsync(user));
    }

    /// <summary>ログイン。成功で httpOnly Cookie にアクセス／リフレッシュトークンを設定する。</summary>
    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user is null)
        {
            return Unauthorized(new { message = "メールアドレスまたはパスワードが正しくありません。" });
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "メールアドレスまたはパスワードが正しくありません。" });
        }

        return Ok(await IssueAuthCookiesAsync(user));
    }

    /// <summary>
    /// リフレッシュトークン Cookie を使ってアクセストークンを再発行する。
    /// 旧リフレッシュトークンは失効させ、新しいものを発行する（ローテーション）。
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<UserDto>> Refresh()
    {
        if (!Request.Cookies.TryGetValue(AuthCookie.RefreshName, out var raw) ||
            string.IsNullOrWhiteSpace(raw))
        {
            return Unauthorized(new { message = "リフレッシュトークンがありません。" });
        }

        var hash = TokenService.HashToken(raw);
        var stored = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash);

        if (stored is null)
        {
            return Unauthorized(new { message = "リフレッシュトークンが無効または失効しています。" });
        }

        // 盗用検知（リフレッシュトークン再利用検知）:
        // 既に失効済み（=ローテーション済み）のトークンが再提示された場合、トークンが盗まれて
        // 攻撃者と正規ユーザーの双方が使っている可能性が高い。安全側に倒し、当該ユーザーの
        // 全リフレッシュトークンを失効させてセッションを強制終了する。
        if (stored.RevokedAt is not null)
        {
            await RevokeAllUserTokensAsync(stored.UserId);
            return Unauthorized(new { message = "リフレッシュトークンが無効または失効しています。" });
        }

        if (!stored.IsActive)
        {
            return Unauthorized(new { message = "リフレッシュトークンが無効または失効しています。" });
        }

        // ローテーション: 旧トークンを失効させ、新しいトークンペアを発行する。
        // 絶対期限はファミリーを通じて引き継ぎ、ローテーションでは延長しない。
        stored.RevokedAt = DateTime.UtcNow;

        return Ok(await IssueAuthCookiesAsync(stored.User, stored.AbsoluteExpiresAt));
    }

    /// <summary>指定ユーザーの未失効リフレッシュトークンをすべて失効させる（盗用検知時のセッション強制終了）。</summary>
    private async Task RevokeAllUserTokensAsync(int userId)
    {
        var now = DateTime.UtcNow;
        var tokens = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.RevokedAt = now;
        }

        await _db.SaveChangesAsync();
    }

    /// <summary>ログアウト。リフレッシュトークンを失効させ、認証 Cookie を破棄する。</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (Request.Cookies.TryGetValue(AuthCookie.RefreshName, out var raw) &&
            !string.IsNullOrWhiteSpace(raw))
        {
            var hash = TokenService.HashToken(raw);
            var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
            if (stored is not null && stored.RevokedAt is null)
            {
                stored.RevokedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        Response.Cookies.Delete(AuthCookie.Name, AuthCookie.BuildDeleteOptions(Request.IsHttps));
        Response.Cookies.Delete(AuthCookie.RefreshName, AuthCookie.BuildDeleteOptions(Request.IsHttps));
        return NoContent();
    }

    /// <summary>現在ログイン中のユーザー情報。トークン検証用。</summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me()
    {
        var id = User.GetUserId();
        if (id is null)
        {
            return Unauthorized();
        }

        var user = await _db.Users.FindAsync(id.Value);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(new UserDto(user.Id, user.Email, user.DisplayName, user.Role));
    }

    /// <summary>
    /// アクセストークン（短命）とリフレッシュトークン（長命）を発行し、いずれも httpOnly Cookie に格納する。
    /// リフレッシュトークンはハッシュのみ DB に保存し、トークン本体はレスポンスボディに含めない。
    /// </summary>
    /// <param name="absoluteExpiresAt">
    /// トークンファミリーの絶対期限。ローテーション時は旧トークンの値を引き継ぎ延長を防ぐ。
    /// 新規ログイン／登録時は null を渡し、現在時刻から既定の絶対寿命で新たに設定する。
    /// </param>
    private async Task<UserDto> IssueAuthCookiesAsync(User user, DateTime? absoluteExpiresAt = null)
    {
        var now = DateTimeOffset.UtcNow;

        // アクセストークン（JWT）
        var accessToken = _tokenService.CreateToken(user);
        var accessExpires = now.AddMinutes(_jwtSettings.ExpiryMinutes);
        Response.Cookies.Append(
            AuthCookie.Name, accessToken, AuthCookie.BuildSetOptions(Request.IsHttps, accessExpires));

        // リフレッシュトークン（ハッシュを永続化、本体は Cookie に格納）
        var (refreshToken, hash) = TokenService.CreateRefreshToken();
        var absolute = absoluteExpiresAt ?? now.AddDays(_tokenService.RefreshTokenAbsoluteDays).UtcDateTime;

        // スライド期限。ただし絶対期限を超えないようにキャップする。
        var refreshExpires = now.AddDays(_tokenService.RefreshTokenDays);
        if (refreshExpires.UtcDateTime > absolute)
        {
            refreshExpires = new DateTimeOffset(absolute, TimeSpan.Zero);
        }

        _db.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = hash,
            UserId = user.Id,
            CreatedAt = now.UtcDateTime,
            ExpiresAt = refreshExpires.UtcDateTime,
            AbsoluteExpiresAt = absolute,
        });
        await _db.SaveChangesAsync();
        Response.Cookies.Append(
            AuthCookie.RefreshName, refreshToken, AuthCookie.BuildSetOptions(Request.IsHttps, refreshExpires));

        return new UserDto(user.Id, user.Email, user.DisplayName, user.Role);
    }
}
