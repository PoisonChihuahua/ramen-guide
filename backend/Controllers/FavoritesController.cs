using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RamenSite.Api.Data;
using RamenSite.Api.Dtos;
using RamenSite.Api.Models;
using RamenSite.Api.Services;

namespace RamenSite.Api.Controllers;

/// <summary>ログインユーザーのお気に入り店舗を管理する。すべて要認証。</summary>
[Authorize]
[ApiController]
[Route("api/favorites")]
public class FavoritesController : ControllerBase
{
    private readonly AppDbContext _db;

    public FavoritesController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>お気に入り登録した店舗一覧を取得する（登録が新しい順）。</summary>
    /// <remarks>
    /// 認証済みユーザー自身のお気に入りのみ返す。各店舗にはレビューの平均評価・件数を含む。
    /// </remarks>
    /// <response code="200">お気に入り店舗一覧（0件の場合は空配列）。</response>
    /// <response code="401">未認証（アクセストークンが未提示または無効）。</response>
    [HttpGet]
    [ProducesResponseType<IEnumerable<ShopDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<ShopDto>>> GetFavorites()
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var shops = await _db.Favorites
            .Where(f => f.UserId == userId.Value)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new ShopDto(
                f.Shop!.Id, f.Shop.Name, f.Shop.Description, f.Shop.Address, f.Shop.Area,
                f.Shop.Genre, f.Shop.OpeningHours, f.Shop.PriceRange, f.Shop.ImageUrl,
                f.Shop.Reviews.Any() ? Math.Round(f.Shop.Reviews.Average(r => r.Rating), 1) : 0,
                f.Shop.Reviews.Count))
            .ToListAsync();

        return Ok(shops);
    }

    /// <summary>指定店舗が自分のお気に入りかどうかを確認する。</summary>
    /// <param name="shopId">確認する店舗の ID。</param>
    /// <response code="200">お気に入り状態を返す。店舗が存在しない場合も IsFavorite: false で返す。</response>
    /// <response code="401">未認証（アクセストークンが未提示または無効）。</response>
    [HttpGet("{shopId:int}/status")]
    [ProducesResponseType<FavoriteStatusDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<FavoriteStatusDto>> GetStatus(int shopId)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var isFavorite = await _db.Favorites
            .AnyAsync(f => f.UserId == userId.Value && f.ShopId == shopId);

        return Ok(new FavoriteStatusDto(shopId, isFavorite));
    }

    /// <summary>店舗をお気に入りに追加する。</summary>
    /// <remarks>
    /// 既に登録済みの場合も成功扱い（冪等）。IsFavorite: true を返す。
    /// </remarks>
    /// <param name="shopId">お気に入りに追加する店舗の ID。</param>
    /// <response code="200">追加成功（既に登録済みの場合も含む）。IsFavorite: true を返す。</response>
    /// <response code="401">未認証（アクセストークンが未提示または無効）。</response>
    /// <response code="404">指定した店舗が存在しない。</response>
    [HttpPut("{shopId:int}")]
    [ProducesResponseType<FavoriteStatusDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FavoriteStatusDto>> AddFavorite(int shopId)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (!await _db.Shops.AnyAsync(s => s.Id == shopId))
        {
            return NotFound();
        }

        var exists = await _db.Favorites
            .AnyAsync(f => f.UserId == userId.Value && f.ShopId == shopId);

        if (!exists)
        {
            _db.Favorites.Add(new Favorite
            {
                UserId = userId.Value,
                ShopId = shopId,
                CreatedAt = DateTime.UtcNow,
            });
            await _db.SaveChangesAsync();
        }

        return Ok(new FavoriteStatusDto(shopId, true));
    }

    /// <summary>お気に入りから外す。</summary>
    /// <remarks>
    /// 未登録の場合も成功扱い（冪等）。IsFavorite: false を返す。
    /// </remarks>
    /// <param name="shopId">お気に入りから外す店舗の ID。</param>
    /// <response code="200">削除成功（未登録の場合も含む）。IsFavorite: false を返す。</response>
    /// <response code="401">未認証（アクセストークンが未提示または無効）。</response>
    [HttpDelete("{shopId:int}")]
    [ProducesResponseType<FavoriteStatusDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<FavoriteStatusDto>> RemoveFavorite(int shopId)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var favorite = await _db.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId.Value && f.ShopId == shopId);

        if (favorite is not null)
        {
            _db.Favorites.Remove(favorite);
            await _db.SaveChangesAsync();
        }

        return Ok(new FavoriteStatusDto(shopId, false));
    }
}
