namespace RamenSite.Api.Models;

/// <summary>
/// リフレッシュトークン。生のトークンは保存せず SHA-256 ハッシュのみを保持する。
/// アクセストークンは短命にし、失効・ローテーションをこのテーブルで管理する。
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }

    /// <summary>生トークンの SHA-256 ハッシュ（16進文字列）。平文は保持しない。</summary>
    public string TokenHash { get; set; } = string.Empty;

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// トークンファミリーの絶対有効期限。ローテーションしても延長されない上限。
    /// これにより、盗まれたリフレッシュトークンを使ったセッションの無期限延命を防ぐ。
    /// </summary>
    public DateTime AbsoluteExpiresAt { get; set; }

    /// <summary>失効日時。null の場合は未失効。ログアウト・ローテーション時に設定する。</summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>未失効かつ、スライド期限・絶対期限のいずれも未経過であれば有効。</summary>
    public bool IsActive =>
        RevokedAt is null && DateTime.UtcNow < ExpiresAt && DateTime.UtcNow < AbsoluteExpiresAt;
}
