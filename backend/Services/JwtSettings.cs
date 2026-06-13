namespace RamenSite.Api.Services;

/// <summary>JWT 設定。署名鍵は appsettings / 環境変数から注入し、コードにハードコードしない。</summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 1440; // 既定: 24時間
}
