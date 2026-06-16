namespace RamenSite.Api.Models;

/// <summary>店舗へのレビュー（星評価＋コメント）。1ユーザーにつき1店舗1件まで。</summary>
public class Review
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User? User { get; set; }

    public int ShopId { get; set; }

    public Shop? Shop { get; set; }

    /// <summary>星評価（1〜5）。</summary>
    public int Rating { get; set; }

    public string Comment { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
