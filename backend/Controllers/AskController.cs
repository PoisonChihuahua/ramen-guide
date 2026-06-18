using Microsoft.AspNetCore.Mvc;
using RamenSite.Api.Dtos;
using RamenSite.Api.Services.Rag;

namespace RamenSite.Api.Controllers;

/// <summary>自然文での店舗検索（RAG）エンドポイント。既存の GET /api/shops とは独立。</summary>
[ApiController]
[Route("api/shops")]
public class AskController : ControllerBase
{
    private const int DefaultTopK = 3;
    private const int MaxTopK = 10;

    private readonly RagSearchService _rag;

    public AskController(RagSearchService rag)
    {
        _rag = rag;
    }

    /// <summary>質問文を受け取り、関連店舗と自然文の回答を返す。</summary>
    [HttpPost("ask")]
    public async Task<ActionResult<AskResponse>> Ask(
        [FromBody] AskRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest(new { message = "質問を入力してください。" });
        }

        var topK = Math.Clamp(request.TopK ?? DefaultTopK, 1, MaxTopK);
        var result = await _rag.AskAsync(request.Question.Trim(), topK, cancellationToken);
        return Ok(result);
    }
}
