namespace RamenSite.Api.Services.Rag;

/// <summary>
/// ベクトル間の類似度計算。RAG の「② Retrieval」で、質問ベクトルと
/// 各店舗ベクトルの「近さ」を測るのに使う。
/// </summary>
public static class VectorMath
{
    /// <summary>
    /// コサイン類似度。2つのベクトルが向いている方向がどれだけ近いかを
    /// -1（真逆）〜 1（同方向）で返す。大きいほど「意味が近い」候補。
    /// </summary>
    public static double Cosine(float[] a, float[] b)
    {
        if (a.Length != b.Length)
        {
            throw new ArgumentException("ベクトルの次元が一致していません。");
        }

        double dot = 0;
        double magA = 0;
        double magB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * (double)b[i];
            magA += a[i] * (double)a[i];
            magB += b[i] * (double)b[i];
        }

        if (magA == 0 || magB == 0)
        {
            return 0; // ゼロベクトルとは「近さ」を定義できない
        }

        return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
    }
}
