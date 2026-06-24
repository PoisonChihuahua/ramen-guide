using System.ComponentModel.DataAnnotations;

namespace RamenSite.Api.Dtos;

/// <summary>ページ分割された一覧レスポンス。</summary>
/// <typeparam name="T">アイテムの型。</typeparam>
/// <param name="Items">現在ページのアイテム一覧。</param>
/// <param name="Total">フィルタ適用後の総件数。</param>
/// <param name="Page">現在のページ番号（1 始まり）。</param>
/// <param name="Limit">1 ページあたりの取得件数。</param>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Total,
    int Page,
    int Limit);

/// <summary>絞り込みドロップダウン用のジャンル・エリア候補。</summary>
/// <param name="Genres">登録済みジャンルの一意リスト（昇順）。</param>
/// <param name="Areas">登録済みエリアの一意リスト（昇順）。</param>
public record ShopOptionsDto(
    IReadOnlyList<string> Genres,
    IReadOnlyList<string> Areas);

/// <summary>店舗一覧・詳細のレスポンス。レビュー集計（平均評価・件数）を含む。</summary>
/// <param name="Id">店舗 ID。</param>
/// <param name="Name">店舗名。</param>
/// <param name="Description">店舗説明。</param>
/// <param name="Address">住所。</param>
/// <param name="Area">エリア（例: 「新宿」「渋谷」）。</param>
/// <param name="Genre">ジャンル（例: 「豚骨」「醤油」）。</param>
/// <param name="OpeningHours">営業時間（例: 「11:00〜23:00」）。</param>
/// <param name="PriceRange">価格帯（例: 「〜1000円」）。</param>
/// <param name="ImageUrl">店舗画像 URL。</param>
/// <param name="AverageRating">レビューの平均評価（1〜5、小数点1桁。レビューなしは 0）。</param>
/// <param name="ReviewCount">レビュー件数。</param>
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
/// <param name="Name">店舗名（必須、100文字以内）。</param>
/// <param name="Description">店舗説明（必須、1000文字以内）。</param>
/// <param name="Address">住所（必須、200文字以内）。</param>
/// <param name="Area">エリア（必須、50文字以内。例: 「新宿」）。</param>
/// <param name="Genre">ジャンル（必須、50文字以内。例: 「豚骨」）。</param>
/// <param name="OpeningHours">営業時間（必須、100文字以内）。</param>
/// <param name="PriceRange">価格帯（必須、50文字以内）。</param>
/// <param name="ImageUrl">店舗画像 URL（必須、500文字以内、URL 形式）。</param>
public record ShopInput(
    [Required, MaxLength(100)] string Name,
    [Required, MaxLength(1000)] string Description,
    [Required, MaxLength(200)] string Address,
    [Required, MaxLength(50)] string Area,
    [Required, MaxLength(50)] string Genre,
    [Required, MaxLength(100)] string OpeningHours,
    [Required, MaxLength(50)] string PriceRange,
    [Required, MaxLength(500), Url] string ImageUrl);
