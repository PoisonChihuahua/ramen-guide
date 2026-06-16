using Microsoft.AspNetCore.Http;

namespace RamenSite.Api.Services;

/// <summary>
/// 認証トークンを格納する httpOnly Cookie の名前と属性を一元管理する。
/// localStorage ではなく httpOnly Cookie に保存することで、JavaScript からトークンを
/// 読み取れなくし（XSS によるトークン窃取を防止）、SameSite=Strict で CSRF を抑止する。
/// </summary>
public static class AuthCookie
{
    public const string Name = "ramensite_auth";

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
        SameSite = SameSiteMode.Strict, // クロスサイトリクエストでは送信しない（CSRF 対策）
        Path = "/",
        Expires = expires,
    };
}
