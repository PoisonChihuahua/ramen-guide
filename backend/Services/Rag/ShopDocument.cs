using RamenSite.Api.Models;

namespace RamenSite.Api.Services.Rag;

/// <summary>
/// 店舗を「埋め込み対象のテキスト」に変換する共通ロジック。
/// 索引化（① Indexing）でベクトル化する元テキストはここに一本化し、
/// pgvector 実装・インメモリ実装のどちらも同じ文章を埋め込むようにする
/// （索引と質問で同じ意味空間に乗せるのが RAG の前提）。
/// </summary>
public static class ShopDocument
{
    /// <summary>名前・ジャンル・エリア・説明をまとめた、埋め込み用の文章。</summary>
    public static string Text(Shop shop) =>
        $"{shop.Name} {shop.Genre} {shop.Area} {shop.Description}";
}
