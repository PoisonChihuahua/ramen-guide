using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Pgvector;
using RamenSite.Api.Data;
using RamenSite.Api.Models;

namespace RamenSite.Api.Services.Rag;

/// <summary>
/// 本番のベクトルストア。索引（① Indexing）と近傍検索（② Retrieval）を
/// Postgres + pgvector で行う。
///
///   ・ <see cref="BuildAsync"/>  … 全店舗を埋め込み、<c>ShopEmbeddings</c>（vector(512)）へ upsert する。
///   ・ <see cref="SearchAsync"/> … コサイン距離演算子 <c>&lt;=&gt;</c> で近い順に取得する。
///                                  類似度 = <c>1 - 距離</c>。並び替えは距離（小さいほど近い）で行う。
///
/// 学習ポイント: インメモリ実装（<see cref="InMemoryVectorStore"/>）は C# 側で全件コサインを回すが、
/// こちらは「並び替え・上位K件の絞り込みを DB（pgvector）に任せる」のが本質的な違い。
/// 件数が増えても HNSW 索引（マイグレーション参照）で近傍検索がスケールする。
/// </summary>
public sealed class PgVectorStore : IShopVectorStore
{
    private readonly AppDbContext _db;
    private readonly IEmbeddingService _embedder;

    public PgVectorStore(AppDbContext db, IEmbeddingService embedder)
    {
        _db = db;
        _embedder = embedder;
    }

    public async Task BuildAsync(IReadOnlyList<Shop> shops, CancellationToken cancellationToken)
    {
        // 既存の埋め込みを引き、店舗ごとに upsert（更新 or 追加）する。
        // 起動のたびに最新のテキストで埋め込み直すため、店舗情報の変更にも追従する。
        var existing = await _db.Set<ShopEmbedding>()
            .ToDictionaryAsync(e => e.ShopId, cancellationToken);

        foreach (var shop in shops)
        {
            var vector = new Vector(_embedder.Embed(ShopDocument.Text(shop)));
            if (existing.TryGetValue(shop.Id, out var row))
            {
                row.Embedding = vector;
            }
            else
            {
                _db.Set<ShopEmbedding>().Add(new ShopEmbedding { ShopId = shop.Id, Embedding = vector });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ShopScore>> SearchAsync(float[] queryVector, int topK, CancellationToken cancellationToken)
    {
        if (queryVector.Length != _embedder.Dimension)
        {
            throw new ArgumentException(
                $"質問ベクトルの次元({queryVector.Length})が索引の次元({_embedder.Dimension})と一致しません。", nameof(queryVector));
        }

        // 近傍検索を SQL 側（pgvector）で行う。<=> はコサイン距離（小さいほど近い）。
        // 類似度として 1 - 距離 を返し、並び替えは距離そのもの（索引が効く形）で行う。
        var connection = (NpgsqlConnection)_db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT "ShopId", 1 - ("Embedding" <=> @q) AS score
                FROM "ShopEmbeddings"
                ORDER BY "Embedding" <=> @q
                LIMIT @k
                """;
            command.Parameters.AddWithValue("q", new Vector(queryVector));
            command.Parameters.AddWithValue("k", topK);

            var results = new List<ShopScore>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var shopId = reader.GetInt32(0);
                var score = reader.GetDouble(1);
                if (score > 0) // 全く重ならない店舗は候補にしない（インメモリ実装と挙動を揃える）
                {
                    results.Add(new ShopScore(shopId, score));
                }
            }

            return results;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }
}
