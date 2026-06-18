using System.ComponentModel.DataAnnotations;

namespace RamenSite.Api.Dtos;

/// <summary>レビュー1件のレスポンス。</summary>
/// <param name="Id">レビュー ID。</param>
/// <param name="ShopId">対象店舗 ID。</param>
/// <param name="UserId">投稿者のユーザー ID。</param>
/// <param name="DisplayName">投稿者の表示名。</param>
/// <param name="Rating">評価（1〜5）。</param>
/// <param name="Comment">コメント（1000文字以内）。</param>
/// <param name="CreatedAt">初回投稿日時（UTC）。</param>
/// <param name="UpdatedAt">最終更新日時（UTC）。</param>
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
/// <param name="Rating">評価（1〜5 の整数）。</param>
/// <param name="Comment">コメント（必須、1000文字以内）。</param>
public record ReviewInput(
    [Range(1, 5)] int Rating,
    [Required, MaxLength(1000)] string Comment);
