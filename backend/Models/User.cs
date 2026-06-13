namespace RamenSite.Api.Models;

/// <summary>一般ユーザー。まずはログイン基盤のみ（お気に入り・レビューは将来追加）。</summary>
public class User
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    /// <summary>ハッシュ化済みパスワード。平文は保持しない。</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
