using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

    /// <summary>新規ユーザー登録。</summary>
    /// <remarks>
    /// メールアドレスを小文字に正規化してから重複チェックを行う。
    /// 登録成功時はアクセストークン（Cookie: access_token）とリフレッシュトークン（Cookie: refresh_token）を
    /// httpOnly Cookie に設定し、ユーザー情報をボディで返す。
    /// </remarks>
    /// <param name="request">登録情報（メールアドレス・パスワード・表示名）。</param>
    /// <response code="200">登録成功。ユーザー情報を返す。認証トークンは httpOnly Cookie に設定済み。</response>
    /// <response code="400">バリデーションエラー（メール形式不正・パスワード短すぎ等）。</response>
    /// <response code="409">指定したメールアドレスは既に登録されている。</response>
    /// <response code="429">レート制限超過。しばらく時間をおいて再試行してください。</response>
    [HttpPost("register")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
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

    /// <summary>ログイン。</summary>
    /// <remarks>
    /// 成功時はアクセストークン（Cookie: access_token）とリフレッシュトークン（Cookie: refresh_token）を
    /// httpOnly Cookie に設定し、ユーザー情報をボディで返す。
    /// ユーザー未存在・パスワード不一致どちらも同一のエラーメッセージを返し、存在有無を秘匿する。
    /// </remarks>
    /// <param name="request">ログイン情報（メールアドレス・パスワード）。</param>
    /// <response code="200">ログイン成功。ユーザー情報を返す。認証トークンは httpOnly Cookie に設定済み。</response>
    /// <response code="400">バリデーションエラー（メール形式不正・パスワード未入力等）。</response>
    /// <response code="401">メールアドレスまたはパスワードが正しくない。</response>
    /// <response code="429">レート制限超過。しばらく時間をおいて再試行してください。</response>
    [HttpPost("login")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
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

    /// <summary>アクセストークン再発行（リフレッシュトークンローテーション）。</summary>
    /// <remarks>
    /// Cookie: refresh_token を使ってアクセストークンを再発行する。
    /// 旧リフレッシュトークンは失効させ、新しいものを発行する（ローテーション）。
    /// 既に失効済みのトークンが提示された場合は盗用と判断し、当該ユーザーの全リフレッシュトークンを
    /// 失効させてセッションを強制終了する。
    /// </remarks>
    /// <response code="200">再発行成功。新しいトークンペアを httpOnly Cookie に設定済み。ユーザー情報を返す。</response>
    /// <response code="401">リフレッシュトークンが未提示・無効・失効している、または盗用が疑われる。</response>
    /// <response code="429">レート制限超過。しばらく時間をおいて再試行してください。</response>
    [HttpPost("refresh")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
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

    /// <summary>ログアウト。</summary>
    /// <remarks>
    /// Cookie: refresh_token を DB 上で失効させ、アクセストークン Cookie とリフレッシュトークン Cookie を削除する。
    /// リフレッシュトークン Cookie が存在しない場合や既に失効済みの場合でも成功扱いとする。
    /// </remarks>
    /// <response code="204">ログアウト成功。認証 Cookie を削除済み。</response>
    /// <response code="429">レート制限超過。しばらく時間をおいて再試行してください。</response>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
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

    /// <summary>現在ログイン中のユーザー情報を取得する。</summary>
    /// <remarks>
    /// アクセストークン（Cookie: access_token または Authorization ヘッダ）を検証し、有効であれば
    /// ユーザー情報を返す。フロントエンドのトークン有効性確認に利用できる。
    /// </remarks>
    /// <response code="200">認証済み。現在のユーザー情報を返す。</response>
    /// <response code="401">アクセストークンが未提示・無効・期限切れ。</response>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    /// <param name="user">トークンを発行するユーザー。</param>
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
