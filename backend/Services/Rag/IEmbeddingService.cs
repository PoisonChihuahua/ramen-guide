namespace RamenSite.Api.Services.Rag;

/// <summary>
/// テキストを固定長のベクトル（埋め込み）に変換する。
/// RAG の「① Indexing / ② Retrieval」工程で、店舗説明文と質問文を
/// 同じベクトル空間に写像するために使う。
///
/// 学習ポイント: この層を差し替え可能なインターフェースにしておくと、
/// キー不要の簡易実装（<see cref="SimpleEmbeddingService"/>）から、
/// Voyage / OpenAI など本物の埋め込みモデルへ「1ファイル差し替え」で移行できる。
/// 本物のモデルは「こってり」と「濃厚な豚骨」のような意味的な近さを捉えられる。
/// </summary>
public interface IEmbeddingService
{
    /// <summary>生成するベクトルの次元数（全テキストで一定）。</summary>
    int Dimension { get; }

    /// <summary>テキストを <see cref="Dimension"/> 次元の L2 正規化済みベクトルに変換する。</summary>
    float[] Embed(string text);
}
