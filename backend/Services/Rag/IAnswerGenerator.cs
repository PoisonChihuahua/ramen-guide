using RamenSite.Api.Dtos;

namespace RamenSite.Api.Services.Rag;

/// <summary>
/// RAG の「③ Generation」工程。検索で引いた店舗（＝根拠）と質問から、
/// 自然文の回答を生成する。
///
/// 学習ポイント: ここを LLM（Claude など）に差し替えると本物の RAG になる。
/// LLM 自体は店舗DBを知らず、② で渡された店舗だけを根拠に答える——これが
/// 「Retrieval-Augmented（検索で補強した）Generation」の核心。
/// 既定の <see cref="TemplateAnswerGenerator"/> はキー不要で動くテンプレ生成。
/// </summary>
public interface IAnswerGenerator
{
    Task<string> GenerateAsync(string question, IReadOnlyList<ShopMatchDto> matches, CancellationToken cancellationToken);
}
