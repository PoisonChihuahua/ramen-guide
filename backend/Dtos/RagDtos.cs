namespace RamenSite.Api.Dtos;

/// <summary>自然文検索（RAG）のリクエスト。</summary>
/// <param name="Question">ユーザーの質問文（例: 「あっさりした塩ラーメンが食べたい」）。</param>
/// <param name="TopK">取得する上位件数。未指定なら既定値を使う。</param>
public record AskRequest(string Question, int? TopK);

/// <summary>検索で引いた店舗1件と、その類似度スコア。</summary>
/// <param name="Shop">店舗情報。</param>
/// <param name="Score">質問とのコサイン類似度（大きいほど関連が高い）。</param>
public record ShopMatchDto(ShopDto Shop, double Score);

/// <summary>自然文検索のレスポンス。</summary>
/// <param name="Question">受け取った質問（トリム済み）。</param>
/// <param name="Answer">取得した店舗を根拠に生成した自然文の回答。</param>
/// <param name="Matches">関連度順の店舗一覧。</param>
public record AskResponse(string Question, string Answer, IReadOnlyList<ShopMatchDto> Matches);
