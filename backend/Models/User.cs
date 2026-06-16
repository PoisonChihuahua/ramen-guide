namespace RamenSite.Api.Models;

/// <summary>一般ユーザー。お気に入り・レビュー投稿の所有者になる。</summary>
public class User
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    /// <summary>ハッシュ化済みパスワード。平文は保持しない。</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>権限ロール。<see cref="UserRoles"/> のいずれか（既定: User）。</summary>
    public string Role { get; set; } = UserRoles.User;

    public DateTime CreatedAt { get; set; }

    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}

/// <summary>ユーザー権限ロールの定数。</summary>
public static class UserRoles
{
    public const string User = "User";

    public const string Admin = "Admin";
}
