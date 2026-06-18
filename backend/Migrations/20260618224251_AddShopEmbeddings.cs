using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace RamenSite.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddShopEmbeddings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "ShopEmbeddings",
                columns: table => new
                {
                    ShopId = table.Column<int>(type: "integer", nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(512)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopEmbeddings", x => x.ShopId);
                    table.ForeignKey(
                        name: "FK_ShopEmbeddings_Shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "Shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // 近傍検索（<=>）を高速化する HNSW 索引。コサイン距離なので vector_cosine_ops を使う。
            // 本プロジェクトは店舗数が少なく無くても正しく動くが、件数増加に備えた学習用の索引。
            // HNSW は空テーブルにも安全に作成できる（IVFFlat と違いデータ投入前でも可）。
            migrationBuilder.Sql(
                """
                CREATE INDEX "IX_ShopEmbeddings_Embedding_hnsw"
                ON "ShopEmbeddings"
                USING hnsw ("Embedding" vector_cosine_ops);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShopEmbeddings");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");
        }
    }
}
