using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RamenSite.Api.Data;
using RamenSite.Api.Dtos;

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
            .Select(s => new ShopDto(
                s.Id, s.Name, s.Description, s.Address, s.Area,
                s.Genre, s.OpeningHours, s.PriceRange, s.ImageUrl))
            .ToListAsync();

        return Ok(shops);
    }

    /// <summary>店舗詳細。</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ShopDto>> GetShop(int id)
    {
        var shop = await _db.Shops.FindAsync(id);
        if (shop is null)
        {
            return NotFound();
        }

        return Ok(ToDto(shop));
    }

    private static ShopDto ToDto(Models.Shop s) => new(
        s.Id, s.Name, s.Description, s.Address, s.Area,
        s.Genre, s.OpeningHours, s.PriceRange, s.ImageUrl);
}
