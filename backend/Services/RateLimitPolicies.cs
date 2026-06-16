namespace RamenSite.Api.Services;

/// <summary>レート制限ポリシー名と設定セクション名の定数。</summary>
public static class RateLimitPolicies
{
    /// <summary>認証エンドポイント（登録・ログイン・リフレッシュ）向けの厳しめのポリシー。</summary>
    public const string Auth = "auth";
}

/// <summary>レート制限の設定（appsettings の "RateLimiting" セクション）。</summary>
public class RateLimitSettings
{
    public const string SectionName = "RateLimiting";

    /// <summary>全エンドポイント共通のグローバル上限（1分あたり、IP 単位）。</summary>
    public int GlobalPermitLimit { get; set; } = 200;

    /// <summary>認証エンドポイントの上限（1分あたり、IP 単位）。総当たり攻撃を抑止する。</summary>
    public int AuthPermitLimit { get; set; } = 30;

    /// <summary>ウィンドウ幅（秒）。</summary>
    public int WindowSeconds { get; set; } = 60;
}
