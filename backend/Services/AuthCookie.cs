using Microsoft.AspNetCore.Http;

namespace RamenSite.Api.Services;

/// <summary>
/// 認証トークンを格納する httpOnly Cookie の名前と属性を一元管理する。
/// localStorage ではなく httpOnly Cookie に保存することで、JavaScript からトークンを
/// 読み取れなくする（XSS によるトークン窃取を防止）。
///
/// SameSite 属性は接続スキームで切り替える:
/// - 本番（HTTPS）: フロントエンド（例 *.insforge.app）とバックエンド（例 *.fly.dev）が
///   別サイトになるため、クロスサイトでも Cookie を送信できるよう SameSite=None を使う
///   （None は Secure とセットが必須）。
/// - ローカル開発（HTTP）: None+Secure が使えないため SameSite=Lax にフォールバックする。
///
/// 注意: SameSite=None は SameSite ベースの CSRF 防御を失う。CORS は許可 Origin を本番ドメインに
/// 限定しているが、書き込みリクエストの CSRF 対策としては別途トークン方式等の導入を検討すること。
/// フロント／バックを同一サイト（同一登録ドメインのサブドメイン）に置けば SameSite=Lax で
/// 防御を維持できる。
/// </summary>
public static class AuthCookie
{
    /// <summary>アクセストークン（JWT）を格納する httpOnly Cookie 名。短命。</summary>
    public const string Name = "ramensite_auth";

    /// <summary>リフレッシュトークンを格納する httpOnly Cookie 名。長命で、失効・ローテーションを DB で管理する。</summary>
    public const string RefreshName = "ramensite_refresh";

    /// <summary>Cookie を発行する際の属性。</summary>
    /// <param name="isHttps">リクエストが HTTPS の場合に Secure 属性を付与する。</param>
    /// <param name="expires">Cookie の有効期限（トークンの有効期限に合わせる）。</param>
    public static CookieOptions BuildSetOptions(bool isHttps, DateTimeOffset expires) =>
        BaseOptions(isHttps, expires);

    /// <summary>Cookie を削除（ログアウト）する際の属性。Set 時と同じ属性で一致させる必要がある。</summary>
    public static CookieOptions BuildDeleteOptions(bool isHttps) =>
        BaseOptions(isHttps, expires: null);

    private static CookieOptions BaseOptions(bool isHttps, DateTimeOffset? expires) => new()
    {
        HttpOnly = true,           // JavaScript からアクセス不可（XSS 対策）
        Secure = isHttps,          // 本番（HTTPS）では Secure。dev の http では送信できるよう false。
        // 本番（HTTPS）はクロスサイトでも送るため None、ローカル http は None 不可のため Lax。
        SameSite = isHttps ? SameSiteMode.None : SameSiteMode.Lax,
        Path = "/",
        Expires = expires,
    };
}
