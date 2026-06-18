using Microsoft.EntityFrameworkCore;
using RamenSite.Api.Data;
using RamenSite.Api.Dtos;

namespace RamenSite.Api.Services.Rag;

/// <summary>
/// RAG の本体。質問文を受けて「② Retrieval（検索）」→「③ Generation（生成）」を行う。
///
///   ① Indexing   … 起動時に <see cref="ShopEmbeddingIndex"/> が全店舗をベクトル化済み
///   ② Retrieval  … 質問をベクトル化 → 全店舗とのコサイン類似度 → 上位K件を取得
///   ③ Generation … 上位K件を根拠に <see cref="IAnswerGenerator"/> が自然文を生成
/// </summary>
public sealed class RagSearchService
{
    private readonly AppDbContext _db;
    private readonly IEmbeddingService _embedder;
    private readonly ShopEmbeddingIndex _index;
    private readonly IAnswerGenerator _generator;

    public RagSearchService(
        AppDbContext db,
        IEmbeddingService embedder,
        ShopEmbeddingIndex index,
        IAnswerGenerator generator)
    {
        _db = db;
        _embedder = embedder;
        _index = index;
        _generator = generator;
    }

    public async Task<AskResponse> AskAsync(string question, int topK, CancellationToken cancellationToken)
    {
        // ② Retrieval: 質問を「店舗と同じベクトル空間」へ写像し、近い順に並べる。
        var queryVector = _embedder.Embed(question);

        var ranked = _index.Vectors
            .Select(v => new { v.ShopId, Score = VectorMath.Cosine(queryVector, v.Vector) })
            .Where(x => x.Score > 0) // 全く重ならない店舗は候補にしない
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .ToList();

        // 取得したIDの店舗をDBから引く（ベクトルにはIDしか持たせていない）。
        // レビュー集計（平均評価・件数）は ShopsController と同じく SQL 側で計算する。
        var ids = ranked.Select(r => r.ShopId).ToList();
        var dtos = await _db.Shops
            .Where(s => ids.Contains(s.Id))
            .Select(s => new ShopDto(
                s.Id, s.Name, s.Description, s.Address, s.Area,
                s.Genre, s.OpeningHours, s.PriceRange, s.ImageUrl,
                s.Reviews.Any() ? Math.Round(s.Reviews.Average(r => r.Rating), 1) : 0,
                s.Reviews.Count))
            .ToListAsync(cancellationToken);

        // ランク順を保ったまま DTO に詰める。
        var matches = ranked
            .Select(r => new ShopMatchDto(dtos.First(d => d.Id == r.ShopId), r.Score))
            .ToList();

        // ③ Generation: 引いた店舗「だけ」を根拠に回答を生成する。
        var answer = await _generator.GenerateAsync(question, matches, cancellationToken);

        return new AskResponse(question, answer, matches);
    }
}
