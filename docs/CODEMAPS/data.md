<!-- Generated: 2026-06-16 | Files scanned: 4 | Token estimate: ~450 -->

# Data Model

EF Core (code-first) + SQLite (`ramensite.db`). Two tables, no relationships yet.

## Tables

### Shops  (`Models/Shop.cs`)

| Column | Type | Notes |
|--------|------|-------|
| Id | int PK | identity |
| Name | string | |
| Description | string | searched by `q` |
| Address | string | searched by `q` |
| Area | string | filter (例: 札幌/東京/横浜/博多) |
| Genre | string | filter (例: 醤油/味噌/豚骨/塩) |
| OpeningHours | string | |
| PriceRange | string | 例: 〜1000円 |
| ImageUrl | string | |
| CreatedAt | DateTime | |

### Users  (`Models/User.cs`)

| Column | Type | Notes |
|--------|------|-------|
| Id | int PK | identity |
| Email | string | **unique index** (IX_Users_Email), lowercased |
| PasswordHash | string | PBKDF2 via IPasswordHasher; no plaintext |
| DisplayName | string | |
| CreatedAt | DateTime | |

## Relationships

None. Users ⟷ Shops not yet linked (お気に入り・レビューは将来追加 per model comments).

## Migrations (`Migrations/`)

```
20260613221316_InitialCreate  — creates Shops, Users + IX_Users_Email
AppDbContextModelSnapshot     — current model snapshot
```

Applied automatically at API startup: `db.Database.Migrate()` (Program.cs).

## Seed (`Data/SeedData.cs`)

`SeedData.Initialize` inserts **6 sample shops** on first boot (skips if `Shops.Any()`). No seeded users.

## See Also

- [backend.md](backend.md)
