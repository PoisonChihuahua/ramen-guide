namespace RamenSite.Api.Dtos;

/// <summary>店舗一覧・詳細のレスポンス。</summary>
public record ShopDto(
    int Id,
    string Name,
    string Description,
    string Address,
    string Area,
    string Genre,
    string OpeningHours,
    string PriceRange,
    string ImageUrl);
