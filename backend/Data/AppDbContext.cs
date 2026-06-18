using Microsoft.EntityFrameworkCore;
using RamenSite.Api.Models;

namespace RamenSite.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Shop> Shops => Set<Shop>();

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Favorite> Favorites => Set<Favorite>();

    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // メールアドレスは一意（重複登録を防ぐ）
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // 既存行・未指定時の既定ロール
        modelBuilder.Entity<User>()
            .Property(u => u.Role)
            .HasDefaultValue(UserRoles.User);

        // リフレッシュトークンはハッシュで検索するためインデックスを張る
        modelBuilder.Entity<RefreshToken>()
            .HasIndex(t => t.TokenHash);

        modelBuilder.Entity<RefreshToken>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // 同じユーザーが同じ店舗を二重にお気に入り登録できない
        modelBuilder.Entity<Favorite>()
            .HasIndex(f => new { f.UserId, f.ShopId })
            .IsUnique();

        modelBuilder.Entity<Favorite>()
            .HasOne(f => f.Shop)
            .WithMany(s => s.Favorites)
            .HasForeignKey(f => f.ShopId)
            .OnDelete(DeleteBehavior.Cascade);

        // 1ユーザーにつき1店舗1レビューまで
        modelBuilder.Entity<Review>()
            .HasIndex(r => new { r.UserId, r.ShopId })
            .IsUnique();

        modelBuilder.Entity<Review>()
            .HasOne(r => r.Shop)
            .WithMany(s => s.Reviews)
            .HasForeignKey(r => r.ShopId)
            .OnDelete(DeleteBehavior.Cascade);

        // RAG の埋め込みベクトルは pgvector（Postgres）専用。Npgsql のときだけモデルに載せる。
        // 統合テストの SQLite には vector 型が無いため、ここで登録しないことで
        // SQLite モデルに ShopEmbeddings を含めない（PgVectorStore も Npgsql 時のみ使われる）。
        if (Database.IsNpgsql())
        {
            modelBuilder.HasPostgresExtension("vector");

            modelBuilder.Entity<ShopEmbedding>(entity =>
            {
                entity.ToTable("ShopEmbeddings");
                entity.HasKey(e => e.ShopId);
                // SimpleEmbeddingService の出力次元(512)に一致させる。次元はモデル変更時に要更新。
                entity.Property(e => e.Embedding).HasColumnType("vector(512)");
                entity.HasOne<Shop>()
                    .WithOne()
                    .HasForeignKey<ShopEmbedding>(e => e.ShopId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
