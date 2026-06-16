namespace RamenSite.Api.Dtos;

/// <summary>お気に入り登録状態のレスポンス。</summary>
public record FavoriteStatusDto(int ShopId, bool IsFavorite);
