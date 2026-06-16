using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RamenSite.Api.Data;
using RamenSite.Api.Dtos;
using RamenSite.Api.Models;
using RamenSite.Api.Services;

namespace RamenSite.Api.Controllers;

/// <summary>店舗ごとのレビュー（星評価＋コメント）。1ユーザーにつき1店舗1件。</summary>
[ApiController]
[Route("api/shops/{shopId:int}/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ReviewsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>店舗のレビュー一覧（新しい順）。公開。</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReviewDto>>> GetReviews(int shopId)
    {
        if (!await _db.Shops.AnyAsync(s => s.Id == shopId))
        {
            return NotFound();
        }

        var reviews = await _db.Reviews
            .Where(r => r.ShopId == shopId)
            .OrderByDescending(r => r.UpdatedAt)
            .Select(r => new ReviewDto(
                r.Id, r.ShopId, r.UserId, r.User!.DisplayName,
                r.Rating, r.Comment, r.CreatedAt, r.UpdatedAt))
            .ToListAsync();

        return Ok(reviews);
    }

    /// <summary>レビューを投稿または更新する（要ログイン）。既存があれば上書き。</summary>
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ReviewDto>> UpsertReview(int shopId, ReviewInput input)
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

        var now = DateTime.UtcNow;
        var review = await _db.Reviews
            .FirstOrDefaultAsync(r => r.ShopId == shopId && r.UserId == userId.Value);

        var isNew = review is null;
        if (review is null)
        {
            review = new Review
            {
                ShopId = shopId,
                UserId = userId.Value,
                CreatedAt = now,
            };
            _db.Reviews.Add(review);
        }

        review.Rating = input.Rating;
        review.Comment = input.Comment.Trim();
        review.UpdatedAt = now;

        await _db.SaveChangesAsync();

        var displayName = await _db.Users
            .Where(u => u.Id == userId.Value)
            .Select(u => u.DisplayName)
            .FirstAsync();

        var dto = new ReviewDto(
            review.Id, review.ShopId, review.UserId, displayName,
            review.Rating, review.Comment, review.CreatedAt, review.UpdatedAt);

        return isNew
            ? CreatedAtAction(nameof(GetReviews), new { shopId }, dto)
            : Ok(dto);
    }

    /// <summary>自分のレビューを削除する（要ログイン）。</summary>
    [Authorize]
    [HttpDelete]
    public async Task<IActionResult> DeleteReview(int shopId)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var review = await _db.Reviews
            .FirstOrDefaultAsync(r => r.ShopId == shopId && r.UserId == userId.Value);

        if (review is null)
        {
            return NotFound();
        }

        _db.Reviews.Remove(review);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
