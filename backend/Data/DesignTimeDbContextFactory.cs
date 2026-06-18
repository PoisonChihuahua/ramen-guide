using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RamenSite.Api.Data;

/// <summary>
/// EF Core ツール（dotnet ef migrations add / database update）が設計時に利用する
/// DbContext ファクトリ。これを提供することで、ツールはアプリのホスト（Program.cs の起動時
/// マイグレーション処理）を実行せずにコンテキストを生成する。
///
/// 接続文字列は環境変数 ConnectionStrings__DefaultConnection から読む。マイグレーションの
/// 生成（add）には実 DB 接続は不要なため、未設定時はプレースホルダにフォールバックする。
/// 実 DB への適用（database update）時のみ、有効な接続文字列を環境変数で渡すこと。
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=insforge;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
