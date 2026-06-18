namespace RamenSite.Api.Services.Rag;

/// <summary>
/// 外部 API キー不要の簡易埋め込み。文字 n-gram（1〜2文字）を
/// 固定長ベクトルのバケットへハッシュ（hashing trick）し、出現回数を数えて
/// L2 正規化する。日本語はトークナイザ無しでも文字 n-gram で扱える。
///
/// 学習上の限界（重要）: これは「文字の重なり」しか見ないので、
/// 「こってり」と「濃厚」のように字面が違う同義表現は近いベクトルにならない。
/// RAG のパイプライン（索引化→ベクトル化→類似度→上位K→生成）を学ぶには十分だが、
/// 本物の意味検索には学習済み埋め込みモデルが要る。その差し替え口が
/// <see cref="IEmbeddingService"/>。
/// </summary>
public sealed class SimpleEmbeddingService : IEmbeddingService
{
    // 次元が大きいほどハッシュ衝突は減るが、データ規模に対して過剰でも意味はない。
    private const int VectorDimension = 512;

    public int Dimension => VectorDimension;

    public float[] Embed(string text)
    {
        var vector = new float[VectorDimension];
        if (string.IsNullOrWhiteSpace(text))
        {
            return vector; // 空テキストはゼロベクトル（類似度は常に 0 になる）
        }

        // 大文字小文字と空白を正規化してから文字を取り出す。
        var chars = new List<char>();
        foreach (var c in text.ToLowerInvariant())
        {
            if (!char.IsWhiteSpace(c))
            {
                chars.Add(c);
            }
        }

        // ① 1-gram（単一文字）と ② 2-gram（隣接2文字）をバケットへ加算。
        for (var i = 0; i < chars.Count; i++)
        {
            AddGram(vector, chars[i].ToString());
            if (i + 1 < chars.Count)
            {
                AddGram(vector, $"{chars[i]}{chars[i + 1]}");
            }
        }

        return L2Normalize(vector);
    }

    private static void AddGram(float[] vector, string gram)
    {
        // 文字列ハッシュは .NET 既定だとプロセス毎にランダム化され再現性がない。
        // RAG では同じ入力が常に同じベクトルになる必要があるので安定ハッシュ(FNV-1a)を使う。
        var bucket = (int)(Fnv1a(gram) % (uint)vector.Length);
        vector[bucket] += 1f;
    }

    private static uint Fnv1a(string text)
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;

        var hash = offsetBasis;
        foreach (var c in text)
        {
            hash ^= c;
            hash *= prime;
        }
        return hash;
    }

    private static float[] L2Normalize(float[] vector)
    {
        double sumSquares = 0;
        foreach (var v in vector)
        {
            sumSquares += v * (double)v;
        }

        var magnitude = Math.Sqrt(sumSquares);
        if (magnitude == 0)
        {
            return vector; // 全ゼロはそのまま
        }

        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] = (float)(vector[i] / magnitude);
        }
        return vector;
    }
}
