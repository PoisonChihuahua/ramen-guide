using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
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

    /// <summary>店舗一覧。キーワード(q)・ジャンル・エリアで絞り込み。</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ShopDto>>> GetShops(
        [FromQuery] string? q,
        [FromQuery] string? genre,
        [FromQuery] string? area)
    {
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

        var shops = await query
            .OrderBy(s => s.Id)
            .Select(ToDto)
            .ToListAsync();

        return Ok(shops);
    }

    /// <summary>店舗詳細。</summary>
    [HttpGet("{id:int}")]
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
    [Authorize(Roles = UserRoles.Admin)]
    [HttpPost]
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
    [Authorize(Roles = UserRoles.Admin)]
    [HttpPut("{id:int}")]
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

    /// <summary>店舗を削除する（管理者のみ）。関連レビュー・お気に入りも連鎖削除される。</summary>
    [Authorize(Roles = UserRoles.Admin)]
    [HttpDelete("{id:int}")]
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
