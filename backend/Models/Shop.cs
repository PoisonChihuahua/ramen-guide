namespace RamenSite.Api.Models;

/// <summary>ラーメン店舗。紹介サイトに表示する基本情報を持つ。</summary>
public class Shop
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    /// <summary>エリア（例: 札幌、横浜、博多）。絞り込みに使用。</summary>
    public string Area { get; set; } = string.Empty;

    /// <summary>ジャンル（例: 醤油、味噌、豚骨、塩）。絞り込みに使用。</summary>
    public string Genre { get; set; } = string.Empty;

    public string OpeningHours { get; set; } = string.Empty;

    /// <summary>価格帯（例: 〜1000円）。</summary>
    public string PriceRange { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
}
