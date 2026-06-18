using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

    /// <summary>店舗のレビュー一覧を取得する（新しい順）。</summary>
    /// <remarks>
    /// 認証不要のパブリックエンドポイント。指定した店舗のすべてのレビューを更新日の降順で返す。
    /// </remarks>
    /// <param name="shopId">レビューを取得する店舗の ID。</param>
    /// <response code="200">レビュー一覧（0件の場合は空配列）。</response>
    /// <response code="404">指定した店舗が存在しない。</response>
    [HttpGet]
    [ProducesResponseType<IEnumerable<ReviewDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    /// <summary>レビューを投稿または更新する（要ログイン）。</summary>
    /// <remarks>
    /// 1ユーザーにつき1店舗1件まで。既存のレビューがある場合は内容を上書き（Upsert）。
    /// 新規投稿時は 201 Created、更新時は 200 OK を返す。
    /// </remarks>
    /// <param name="shopId">レビューを投稿する店舗の ID。</param>
    /// <param name="input">レビュー内容（評価: 1〜5、コメント: 1000文字以内）。</param>
    /// <response code="201">新規投稿成功。投稿したレビューを返す。</response>
    /// <response code="200">更新成功（既存レビューを上書き）。更新後のレビューを返す。</response>
    /// <response code="400">バリデーションエラー（評価範囲外・コメント未入力等）。</response>
    /// <response code="401">未認証（アクセストークンが未提示または無効）。</response>
    /// <response code="404">指定した店舗が存在しない。</response>
    [Authorize]
    [HttpPost]
    [ProducesResponseType<ReviewDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ReviewDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    /// <remarks>
    /// 認証済みユーザー自身が投稿したレビューのみ削除可能。他ユーザーのレビューは削除できない。
    /// </remarks>
    /// <param name="shopId">レビューを削除する店舗の ID。</param>
    /// <response code="204">削除成功。</response>
    /// <response code="401">未認証（アクセストークンが未提示または無効）。</response>
    /// <response code="404">指定した店舗に自分のレビューが存在しない。</response>
    [Authorize]
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
