using Microsoft.AspNetCore.Identity;
using RamenSite.Api.Models;

namespace RamenSite.Api.Data;

/// <summary>初回起動時にサンプルのラーメン店データと管理者ユーザーを投入する。</summary>
public static class SeedData
{
    public static void Initialize(AppDbContext db, bool isDevelopment)
    {
        SeedAdminUser(db, isDevelopment);

        if (db.Shops.Any())
        {
            return; // 既にデータがあれば何もしない
        }

        var now = DateTime.UtcNow;
        var shops = new[]
        {
            new Shop
            {
                Name = "麺屋 こく一番",
                Description = "濃厚な豚骨スープが自慢の老舗。替え玉無料。",
                Address = "福岡県福岡市博多区博多駅前1-1-1",
                Area = "博多",
                Genre = "豚骨",
                OpeningHours = "11:00〜23:00",
                PriceRange = "〜1000円",
                ImageUrl = "https://images.unsplash.com/photo-1557872943-16a5ac26437e?w=800",
                CreatedAt = now,
            },
            new Shop
            {
                Name = "札幌味噌堂",
                Description = "コクのある味噌ラーメン。バターコーンが人気。",
                Address = "北海道札幌市中央区南3条西4丁目",
                Area = "札幌",
                Genre = "味噌",
                OpeningHours = "11:00〜22:00",
                PriceRange = "1000〜1500円",
                ImageUrl = "https://images.unsplash.com/photo-1623341214825-9f4f963727da?w=800",
                CreatedAt = now,
            },
            new Shop
            {
                Name = "中華そば 鶏清",
                Description = "あっさり鶏清湯の醤油ラーメン。自家製麺。",
                Address = "東京都千代田区神田小川町2-2-2",
                Area = "東京",
                Genre = "醤油",
                OpeningHours = "11:30〜15:00, 18:00〜21:00",
                PriceRange = "〜1000円",
                ImageUrl = "https://images.unsplash.com/photo-1591814468924-caf88d1232e1?w=800",
                CreatedAt = now,
            },
            new Shop
            {
                Name = "塩らーめん 海凪",
                Description = "魚介出汁の透き通った塩スープ。",
                Address = "神奈川県横浜市西区南幸1-3-3",
                Area = "横浜",
                Genre = "塩",
                OpeningHours = "11:00〜21:00",
                PriceRange = "1000〜1500円",
                ImageUrl = "https://images.unsplash.com/photo-1569718212165-3a8278d5f624?w=800",
                CreatedAt = now,
            },
            new Shop
            {
                Name = "二郎系 豪快家",
                Description = "ボリューム満点の極太麺と背脂。",
                Address = "東京都豊島区南池袋2-4-4",
                Area = "東京",
                Genre = "豚骨醤油",
                OpeningHours = "11:00〜23:00",
                PriceRange = "〜1000円",
                ImageUrl = "https://images.unsplash.com/photo-1632709810780-b5a4343cebec?w=800",
                CreatedAt = now,
            },
            new Shop
            {
                Name = "味噌乃 北大前店",
                Description = "辛味噌が選べる。学生に人気のボリューム系。",
                Address = "北海道札幌市北区北18条西5丁目",
                Area = "札幌",
                Genre = "味噌",
                OpeningHours = "11:00〜21:30",
                PriceRange = "〜1000円",
                ImageUrl = "https://images.unsplash.com/photo-1612929633738-8fe44f7ec841?w=800",
                CreatedAt = now,
            },
        };

        db.Shops.AddRange(shops);
        db.SaveChanges();

        SeedSampleReviews(db, now);
    }

    /// <summary>
    /// 管理者ユーザーを投入する。資格情報は環境変数で上書きする。
    /// SEED_ADMIN_EMAIL / SEED_ADMIN_PASSWORD。
    /// 本番では既定値での起動を許可せず、未設定なら例外で起動を中断する
    /// （既定の管理者資格情報による乗っ取りを防ぐため）。開発時のみ既定値を使う。
    /// </summary>
    private static void SeedAdminUser(AppDbContext db, bool isDevelopment)
    {
        var email = (Environment.GetEnvironmentVariable("SEED_ADMIN_EMAIL")
                     ?? "admin@ramen.test").Trim().ToLowerInvariant();

        if (db.Users.Any(u => u.Email == email))
        {
            return;
        }

        var password = Environment.GetEnvironmentVariable("SEED_ADMIN_PASSWORD");
        if (string.IsNullOrWhiteSpace(password))
        {
            if (!isDevelopment)
            {
                throw new InvalidOperationException(
                    "本番環境では SEED_ADMIN_PASSWORD（および推奨で SEED_ADMIN_EMAIL）を設定してください。" +
                    "既定の管理者資格情報での起動は許可されていません。");
            }

            password = "adminpass123"; // ローカル開発専用の既定値
        }

        var admin = new User
        {
            Email = email,
            DisplayName = "管理者",
            Role = UserRoles.Admin,
            CreatedAt = DateTime.UtcNow,
        };
        admin.PasswordHash = new PasswordHasher<User>().HashPassword(admin, password);

        db.Users.Add(admin);
        db.SaveChanges();
    }

    /// <summary>サンプルのレビューを数件投入する（管理者を投稿者として利用）。</summary>
    private static void SeedSampleReviews(AppDbContext db, DateTime now)
    {
        var admin = db.Users.FirstOrDefault(u => u.Role == UserRoles.Admin);
        if (admin is null)
        {
            return;
        }

        var firstShop = db.Shops.OrderBy(s => s.Id).FirstOrDefault();
        if (firstShop is null)
        {
            return;
        }

        db.Reviews.Add(new Review
        {
            ShopId = firstShop.Id,
            UserId = admin.Id,
            Rating = 5,
            Comment = "濃厚なのに最後まで飲み干せるスープ。替え玉無料も嬉しい。",
            CreatedAt = now,
            UpdatedAt = now,
        });
        db.SaveChanges();
    }
}
