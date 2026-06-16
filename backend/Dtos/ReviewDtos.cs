using System.ComponentModel.DataAnnotations;

namespace RamenSite.Api.Dtos;

/// <summary>レビュー1件のレスポンス。</summary>
public record ReviewDto(
    int Id,
    int ShopId,
    int UserId,
    string DisplayName,
    int Rating,
    string Comment,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>レビュー投稿・更新リクエスト。</summary>
public record ReviewInput(
    [Range(1, 5)] int Rating,
    [Required, MaxLength(1000)] string Comment);
