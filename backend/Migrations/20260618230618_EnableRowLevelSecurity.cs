using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RamenSite.Api.Migrations
{
    /// <inheritdoc />
    public partial class EnableRowLevelSecurity : Migration
    {
        // InsForge は public スキーマの全テーブルを PostgREST(REST API)として自動公開し、
        // RLS(行レベルセキュリティ)で保護する。RLS が無効だと公開 anon キーだけで誰でも
        // read/write できてしまう（ASP.NET バックエンドを迂回）。
        //
        // このアプリは PostgREST/anon 経路を使わず、ASP.NET が DSN で直結する構成。
        // そこで全テーブルで RLS を有効化し「ポリシー無し＝anon/authenticated は全拒否」にする。
        // バックエンドの DSN ロールはテーブル所有者（EF が CREATE TABLE した本人）であり、
        // FORCE しない限り所有者は RLS をバイパスするため、アプリの動作には影響しない。
        private static readonly string[] Tables =
        {
            "Shops",
            "Users",
            "RefreshTokens",
            "Favorites",
            "Reviews",
            "ShopEmbeddings",
            "__EFMigrationsHistory",
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" ENABLE ROW LEVEL SECURITY;");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" DISABLE ROW LEVEL SECURITY;");
            }
        }
    }
}
