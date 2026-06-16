using System.ComponentModel.DataAnnotations;

namespace RamenSite.Api.Dtos;

/// <summary>店舗一覧・詳細のレスポンス。レビュー集計（平均・件数）を含む。</summary>
public record ShopDto(
    int Id,
    string Name,
    string Description,
    string Address,
    string Area,
    string Genre,
    string OpeningHours,
    string PriceRange,
    string ImageUrl,
    double AverageRating,
    int ReviewCount);

/// <summary>店舗の新規作成・更新リクエスト（管理者用）。</summary>
public record ShopInput(
    [Required, MaxLength(100)] string Name,
    [Required, MaxLength(1000)] string Description,
    [Required, MaxLength(200)] string Address,
    [Required, MaxLength(50)] string Area,
    [Required, MaxLength(50)] string Genre,
    [Required, MaxLength(100)] string OpeningHours,
    [Required, MaxLength(50)] string PriceRange,
    [Required, MaxLength(500), Url] string ImageUrl);
