using System.ComponentModel.DataAnnotations;

namespace RamenSite.Api.Dtos;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required, MaxLength(50)] string DisplayName);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

/// <summary>リフレッシュ／ログアウト時に生のリフレッシュトークンを受け取る。</summary>
public record RefreshRequest(string RefreshToken);

/// <summary>ログイン/登録/リフレッシュ成功時のレスポンス。</summary>
public record AuthResponse(string Token, string RefreshToken, UserDto User);

public record UserDto(int Id, string Email, string DisplayName);
