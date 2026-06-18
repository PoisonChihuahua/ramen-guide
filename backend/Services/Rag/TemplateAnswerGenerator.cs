using System.Text;
using RamenSite.Api.Dtos;

namespace RamenSite.Api.Services.Rag;

/// <summary>
/// キー不要のテンプレ回答生成。検索で引いた店舗を整形して自然文っぽくまとめる
/// （LLM は呼ばない）。RAG パイプラインをキー無しで一通り動かすための既定実装。
///
/// === ここを本物の LLM 生成に差し替える方法（学習メモ） ===
/// Anthropic C# SDK を入れて ClaudeAnswerGenerator を作り、DI で差し替えるだけ:
///
///   dotnet add package Anthropic
///
///   public sealed class ClaudeAnswerGenerator(AnthropicClient client) : IAnswerGenerator
///   {
///       public async Task&lt;string&gt; GenerateAsync(string question,
///           IReadOnlyList&lt;ShopMatchDto&gt; matches, CancellationToken ct)
///       {
///           // ② で引いた店舗だけをコンテキスト(=根拠)として渡す。LLM は DB を知らない。
///           var context = string.Join("\n", matches.Select(m =>
///               $"- {m.Shop.Name}（{m.Shop.Genre}/{m.Shop.Area}）: {m.Shop.Description}"));
///           var prompt = $"次の候補だけを根拠に、質問へ日本語で簡潔に答えて。\n" +
///                        $"質問: {question}\n候補:\n{context}";
///           var res = await client.Messages.Create(new MessageCreateParams
///           {
///               Model = Model.ClaudeOpus4_8,   // 文字列なら "claude-opus-4-8"
///               MaxTokens = 1024,
///               Messages = [new() { Role = Role.User, Content = prompt }],
///           });
///           return string.Concat(res.Content.Select(b =&gt; b.Value)
///               .OfType&lt;TextBlock&gt;().Select(t =&gt; t.Text));
///       }
///   }
///
/// この差し替えに ANTHROPIC_API_KEY が必要なため、既定はテンプレ生成にしている。
/// </summary>
public sealed class TemplateAnswerGenerator : IAnswerGenerator
{
    public Task<string> GenerateAsync(string question, IReadOnlyList<ShopMatchDto> matches, CancellationToken cancellationToken)
    {
        if (matches.Count == 0)
        {
            return Task.FromResult("ご希望に合いそうな店舗が見つかりませんでした。別の言葉で試してみてください。");
        }

        var top = matches[0].Shop;
        var sb = new StringBuilder();
        sb.Append($"「{question}」には、まず「{top.Name}」（{top.Genre}・{top.Area}）がおすすめです。{top.Description}");

        if (matches.Count > 1)
        {
            var others = string.Join("、", matches.Skip(1).Select(m => $"「{m.Shop.Name}」({m.Shop.Genre})"));
            sb.Append($" ほかに {others} も関連度が高めです。");
        }

        return Task.FromResult(sb.ToString());
    }
}
