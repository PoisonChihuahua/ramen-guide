<!-- Generated: 2026-06-16 | Files scanned: 18 | Token estimate: ~650 -->

# Backend Architecture

ASP.NET Core (.NET 10) Web API. Controller → EF Core `AppDbContext` (no separate service/repo layer; thin controllers).

## Entry Point

`backend/Program.cs` — DI wiring (DbContext, PasswordHasher, TokenService, JWT auth, CORS, Swagger), boot-time `db.Database.Migrate()` + `SeedData.Initialize`. Exposes `partial class Program` for integration tests.

## Routes

```
GET  /api/shops?q&genre&area → ShopsController.GetShops → EF LINQ filter → ShopDto[]
GET  /api/shops/{id:int}     → ShopsController.GetShop  → Shops.FindAsync → ShopDto | 404

POST /api/auth/register      → AuthController.Register → hash pw, insert User → AuthResponse(token,user) | 409 dup
POST /api/auth/login         → AuthController.Login    → verify pw → AuthResponse | 401
GET  /api/auth/me  [Authorize] → AuthController.Me     → claims→User → UserDto | 401
```

## Middleware Chain (Program.cs)

```
[dev] Swagger → CORS(FrontendCors) → Authentication(JWT) → Authorization → MapControllers
```

## Key Files

| File | Role | ~Lines |
|------|------|--------|
| `Program.cs` | bootstrap, DI, pipeline | 110 |
| `Controllers/ShopsController.cs` | list + detail, query filtering | 90 |
| `Controllers/AuthController.cs` | register/login/me, pw hashing | 110 |
| `Services/TokenService.cs` | builds JWT (sub/email/displayName claims) | 45 |
| `Services/JwtSettings.cs` | Options-bound `Jwt` config (Key/Issuer/Audience/Expiry) | 18 |
| `Data/AppDbContext.cs` | DbSets Shops/Users, unique Email index | 30 |
| `Data/SeedData.cs` | 6 sample shops on first boot | — |
| `Dtos/ShopDtos.cs` | `ShopDto` record | 12 |
| `Dtos/AuthDtos.cs` | Register/Login requests, AuthResponse, UserDto (DataAnnotations) | 18 |

## Patterns

- Thin controllers, direct `AppDbContext` access (no repository layer — YAGNI for current scope).
- DTOs are `record` types; entities never returned directly.
- Password hashing via built-in `IPasswordHasher<User>` (PBKDF2).
- JWT signing key injected via config — **never hardcoded** (`Jwt:Key`).
- Email normalized to lowercase before lookup/insert.

## Dependencies (csproj)

EF Core Sqlite 10.0.9 · JwtBearer 10.0.9 · Swashbuckle 10.2.1 · EF Core Design (migrations).

## See Also

- [data.md](data.md) · [architecture.md](architecture.md)
