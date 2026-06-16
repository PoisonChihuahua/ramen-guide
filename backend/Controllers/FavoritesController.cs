using Microsoft.AspNetCore.Authorization;
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

    /// <summary>お気に入り登録した店舗一覧（登録が新しい順）。</summary>
    [HttpGet]
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

    /// <summary>指定店舗が自分のお気に入りかどうか。</summary>
    [HttpGet("{shopId:int}/status")]
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

    /// <summary>店舗をお気に入りに追加する。既に登録済みでも成功扱い（冪等）。</summary>
    [HttpPut("{shopId:int}")]
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

    /// <summary>お気に入りから外す。未登録でも成功扱い（冪等）。</summary>
    [HttpDelete("{shopId:int}")]
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
