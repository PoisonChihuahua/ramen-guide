using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RamenSite.Api.Data;
using RamenSite.Api.Dtos;
using RamenSite.Api.Models;

namespace RamenSite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShopsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ShopsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>店舗一覧を取得する（ページネーション対応）。</summary>
    /// <remarks>
    /// キーワード・ジャンル・エリアで絞り込みができる。結果は ID 昇順。
    /// 各店舗にはレビューの平均評価・件数を集計済みの状態で含む。
    /// </remarks>
    /// <param name="q">キーワード検索。店舗名・説明・住所に対して部分一致検索する。</param>
    /// <param name="genre">ジャンルで絞り込む（例: 「豚骨」「醤油」）。完全一致。</param>
    /// <param name="area">エリアで絞り込む（例: 「新宿」「渋谷」）。完全一致。</param>
    /// <param name="page">取得するページ番号（1 始まり、既定 1）。</param>
    /// <param name="limit">1 ページあたりの取得件数（1〜100、既定 20）。</param>
    /// <response code="200">ページ分割された店舗一覧。</response>
    [HttpGet]
    [ProducesResponseType<PagedResult<ShopDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ShopDto>>> GetShops(
        [FromQuery] string? q,
        [FromQuery] string? genre,
        [FromQuery] string? area,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20)
    {
        page = Math.Max(1, page);
        limit = Math.Clamp(limit, 1, 100);

        var query = _db.Shops.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(s =>
                s.Name.Contains(keyword) ||
                s.Description.Contains(keyword) ||
                s.Address.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(genre))
        {
            query = query.Where(s => s.Genre == genre);
        }

        if (!string.IsNullOrWhiteSpace(area))
        {
            query = query.Where(s => s.Area == area);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(s => s.Id)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(ToDto)
            .ToListAsync();

        return Ok(new PagedResult<ShopDto>(items, total, page, limit));
    }

    /// <summary>絞り込みドロップダウン用のジャンル・エリア候補を取得する。</summary>
    /// <response code="200">登録済みジャンルとエリアの一意リスト。</response>
    [HttpGet("options")]
    [ProducesResponseType<ShopOptionsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ShopOptionsDto>> GetShopOptions()
    {
        var genres = await _db.Shops
            .Select(s => s.Genre)
            .Distinct()
            .OrderBy(g => g)
            .ToListAsync();

        var areas = await _db.Shops
            .Select(s => s.Area)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();

        return Ok(new ShopOptionsDto(genres, areas));
    }

    /// <summary>店舗詳細を取得する。</summary>
    /// <param name="id">店舗 ID。</param>
    /// <response code="200">店舗詳細（レビュー平均評価・件数を含む）。</response>
    /// <response code="404">指定した店舗が存在しない。</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ShopDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShopDto>> GetShop(int id)
    {
        var shop = await _db.Shops
            .Where(s => s.Id == id)
            .Select(ToDto)
            .FirstOrDefaultAsync();

        if (shop is null)
        {
            return NotFound();
        }

        return Ok(shop);
    }

    /// <summary>店舗を新規登録する（管理者のみ）。</summary>
    /// <remarks>
    /// 管理者ロール（Role: admin）を持つユーザーのみ実行可能。
    /// 登録成功時は 201 Created を返し、Location ヘッダに詳細取得 URL を設定する。
    /// </remarks>
    /// <param name="input">店舗情報（名前・説明・住所・エリア・ジャンル・営業時間・価格帯・画像 URL）。</param>
    /// <response code="201">登録成功。登録した店舗情報を返す。</response>
    /// <response code="400">バリデーションエラー（必須項目未入力・URL 形式不正等）。</response>
    /// <response code="401">未認証（アクセストークンが未提示または無効）。</response>
    /// <response code="403">権限不足（管理者ロールが必要）。</response>
    [Authorize(Roles = UserRoles.Admin)]
    [HttpPost]
    [ProducesResponseType<ShopDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ShopDto>> CreateShop(ShopInput input)
    {
        var shop = new Shop
        {
            Name = input.Name.Trim(),
            Description = input.Description.Trim(),
            Address = input.Address.Trim(),
            Area = input.Area.Trim(),
            Genre = input.Genre.Trim(),
            OpeningHours = input.OpeningHours.Trim(),
            PriceRange = input.PriceRange.Trim(),
            ImageUrl = input.ImageUrl.Trim(),
            CreatedAt = DateTime.UtcNow,
        };

        _db.Shops.Add(shop);
        await _db.SaveChangesAsync();

        var dto = new ShopDto(
            shop.Id, shop.Name, shop.Description, shop.Address, shop.Area,
            shop.Genre, shop.OpeningHours, shop.PriceRange, shop.ImageUrl, 0, 0);

        return CreatedAtAction(nameof(GetShop), new { id = shop.Id }, dto);
    }

    /// <summary>店舗情報を更新する（管理者のみ）。</summary>
    /// <remarks>
    /// 管理者ロール（Role: admin）を持つユーザーのみ実行可能。
    /// 全フィールドを上書きする（PATCH ではなく PUT）。レビュー集計は DB から再計算して返す。
    /// </remarks>
    /// <param name="id">更新する店舗の ID。</param>
    /// <param name="input">更新後の店舗情報。</param>
    /// <response code="200">更新成功。更新後の店舗情報（レビュー集計を含む）を返す。</response>
    /// <response code="400">バリデーションエラー（必須項目未入力・URL 形式不正等）。</response>
    /// <response code="401">未認証（アクセストークンが未提示または無効）。</response>
    /// <response code="403">権限不足（管理者ロールが必要）。</response>
    /// <response code="404">指定した店舗が存在しない。</response>
    [Authorize(Roles = UserRoles.Admin)]
    [HttpPut("{id:int}")]
    [ProducesResponseType<ShopDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShopDto>> UpdateShop(int id, ShopInput input)
    {
        var shop = await _db.Shops.FindAsync(id);
        if (shop is null)
        {
            return NotFound();
        }

        shop.Name = input.Name.Trim();
        shop.Description = input.Description.Trim();
        shop.Address = input.Address.Trim();
        shop.Area = input.Area.Trim();
        shop.Genre = input.Genre.Trim();
        shop.OpeningHours = input.OpeningHours.Trim();
        shop.PriceRange = input.PriceRange.Trim();
        shop.ImageUrl = input.ImageUrl.Trim();

        await _db.SaveChangesAsync();

        var dto = await _db.Shops
            .Where(s => s.Id == id)
            .Select(ToDto)
            .FirstAsync();

        return Ok(dto);
    }

    /// <summary>店舗を削除する（管理者のみ）。</summary>
    /// <remarks>
    /// 管理者ロール（Role: admin）を持つユーザーのみ実行可能。
    /// 関連するレビュー・お気に入りも DB の CASCADE により連鎖削除される。
    /// </remarks>
    /// <param name="id">削除する店舗の ID。</param>
    /// <response code="204">削除成功。</response>
    /// <response code="401">未認証（アクセストークンが未提示または無効）。</response>
    /// <response code="403">権限不足（管理者ロールが必要）。</response>
    /// <response code="404">指定した店舗が存在しない。</response>
    [Authorize(Roles = UserRoles.Admin)]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteShop(int id)
    {
        var shop = await _db.Shops.FindAsync(id);
        if (shop is null)
        {
            return NotFound();
        }

        _db.Shops.Remove(shop);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>店舗エンティティを集計付き DTO へ射影する式（EF Core で SQL に変換される）。</summary>
    private static readonly Expression<Func<Shop, ShopDto>> ToDto = s => new ShopDto(
        s.Id, s.Name, s.Description, s.Address, s.Area,
        s.Genre, s.OpeningHours, s.PriceRange, s.ImageUrl,
        s.Reviews.Any() ? Math.Round(s.Reviews.Average(r => r.Rating), 1) : 0,
        s.Reviews.Count);
}
