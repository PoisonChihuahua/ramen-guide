namespace RamenSite.Api.Dtos;

/// <summary>お気に入り登録状態のレスポンス。</summary>
/// <param name="ShopId">対象店舗 ID。</param>
/// <param name="IsFavorite">お気に入りに登録されているかどうか。</param>
public record FavoriteStatusDto(int ShopId, bool IsFavorite);
