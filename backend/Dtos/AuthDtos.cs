using System.ComponentModel.DataAnnotations;

namespace RamenSite.Api.Dtos;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required, MaxLength(50)] string DisplayName);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

/// <summary>ログイン/登録成功時のレスポンス。</summary>
public record AuthResponse(string Token, UserDto User);

public record UserDto(int Id, string Email, string DisplayName);
