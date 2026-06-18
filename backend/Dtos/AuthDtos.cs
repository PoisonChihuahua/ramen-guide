using System.ComponentModel.DataAnnotations;

namespace RamenSite.Api.Dtos;

/// <summary>ユーザー登録リクエスト。</summary>
/// <param name="Email">メールアドレス（必須、形式チェックあり）。登録時に小文字に正規化される。</param>
/// <param name="Password">パスワード（必須、8文字以上）。</param>
/// <param name="DisplayName">表示名（必須、50文字以内）。</param>
public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required, MaxLength(50)] string DisplayName);

/// <summary>ログインリクエスト。</summary>
/// <param name="Email">登録時のメールアドレス。</param>
/// <param name="Password">パスワード。</param>
public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

/// <summary>ログイン・登録・ユーザー照会のレスポンス。トークンは httpOnly Cookie で送られるためボディには含めない。</summary>
/// <param name="Id">ユーザー ID。</param>
/// <param name="Email">メールアドレス（小文字正規化済み）。</param>
/// <param name="DisplayName">表示名。</param>
/// <param name="Role">ロール（"user" または "admin"）。</param>
public record UserDto(int Id, string Email, string DisplayName, string Role);
