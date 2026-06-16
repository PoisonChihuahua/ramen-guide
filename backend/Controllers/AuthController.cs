using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RamenSite.Api.Data;
using RamenSite.Api.Dtos;
using RamenSite.Api.Models;
using RamenSite.Api.Services;

namespace RamenSite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
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

    /// <summary>ユーザー登録。成功で httpOnly Cookie に JWT を設定する。</summary>
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

        return Ok(IssueAuthCookie(user));
    }

    /// <summary>ログイン。成功で httpOnly Cookie に JWT を設定する。</summary>
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

        return Ok(IssueAuthCookie(user));
    }

    /// <summary>ログアウト。認証 Cookie を破棄する。</summary>
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(AuthCookie.Name, AuthCookie.BuildDeleteOptions(Request.IsHttps));
        return NoContent();
    }

    /// <summary>現在ログイン中のユーザー情報。トークン検証用。</summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub");

        if (!int.TryParse(userId, out var id))
        {
            return Unauthorized();
        }

        var user = await _db.Users.FindAsync(id);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(new UserDto(user.Id, user.Email, user.DisplayName));
    }

    /// <summary>JWT を発行し httpOnly Cookie に格納する。トークンはレスポンスボディに含めない。</summary>
    private UserDto IssueAuthCookie(User user)
    {
        var token = _tokenService.CreateToken(user);
        var expires = DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);
        Response.Cookies.Append(AuthCookie.Name, token, AuthCookie.BuildSetOptions(Request.IsHttps, expires));
        return new UserDto(user.Id, user.Email, user.DisplayName);
    }
}
