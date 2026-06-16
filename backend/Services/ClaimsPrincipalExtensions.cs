using System.Security.Claims;

namespace RamenSite.Api.Services;

/// <summary>認証済みユーザーのクレームから値を取り出すヘルパー。</summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>JWT の subject からユーザーIDを取得する。取得できない場合は null。</summary>
    public static int? GetUserId(this ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? principal.FindFirstValue("sub");

        return int.TryParse(raw, out var id) ? id : null;
    }
}
