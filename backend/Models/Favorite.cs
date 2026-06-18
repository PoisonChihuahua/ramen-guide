namespace RamenSite.Api.Models;

/// <summary>ユーザーが店舗をお気に入り登録した関連。(UserId, ShopId) で一意。</summary>
public class Favorite
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User? User { get; set; }

    public int ShopId { get; set; }

    public Shop? Shop { get; set; }

    public DateTime CreatedAt { get; set; }
}
