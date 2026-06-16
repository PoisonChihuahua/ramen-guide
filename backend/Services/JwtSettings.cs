namespace RamenSite.Api.Services;

/// <summary>JWT 設定。署名鍵は appsettings / 環境変数から注入し、コードにハードコードしない。</summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 15; // 既定: アクセストークンは短命（15分）

    public int RefreshTokenDays { get; set; } = 7; // 既定: リフレッシュトークンは7日有効
}
