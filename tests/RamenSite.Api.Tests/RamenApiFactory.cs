using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RamenSite.Api.Data;

namespace RamenSite.Api.Tests;

/// <summary>
/// 統合テスト用のアプリ起動ファクトリ。
/// 本番の SQLite ファイルではなく、接続を開いたままのインメモリ SQLite に差し替える。
/// 起動時のマイグレーション適用＋シードはそのまま流用される。
/// </summary>
public class RamenApiFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // テスト用の JWT 設定（環境に依存せず必須キーを満たす）
        builder.UseSetting("Jwt:Key", "integration-test-signing-key-at-least-32-chars!!");
        builder.UseSetting("Jwt:Issuer", "RamenSiteTest");
        builder.UseSetting("Jwt:Audience", "RamenSiteTestClient");

        builder.ConfigureServices(services =>
        {
            // 既存の AppDbContext 関連登録を取り除く
            var toRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType == typeof(AppDbContext))
                .ToList();
            foreach (var descriptor in toRemove)
            {
                services.Remove(descriptor);
            }

            // 開いたまま保持することで、インメモリ DB がテスト中に消えないようにする
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection?.Dispose();
        }
    }
}
