<!-- Generated: 2026-06-16 | Files scanned: 5 | Token estimate: ~500 -->

# Dependencies & Integrations

Self-contained app — no external/third-party services. SQLite is the only data store; all assets are local or Unsplash image URLs.

## External Services

| Service | Use | Notes |
|---------|-----|-------|
| SQLite (file) | primary data store | `ramensite.db`, local file — no server |
| Unsplash (images) | shop/hero photos | plain `<img src>` URLs in seed + ShopListPage hero |

No payment, email, cache (Redis), or auth provider. JWT issued in-process.

## Backend Packages (`RamenSite.Api.csproj`, .NET 10)

| Package | Ver | Purpose |
|---------|-----|---------|
| Microsoft.EntityFrameworkCore.Sqlite | 10.0.9 | ORM + SQLite provider |
| Microsoft.EntityFrameworkCore.Design | 10.0.9 | migrations tooling |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.9 | JWT validation |
| Microsoft.AspNetCore.OpenApi | 10.0.5 | OpenAPI |
| Swashbuckle.AspNetCore | 10.2.1 | Swagger UI (dev) |

## Frontend Packages (`frontend/package.json`)

Runtime: `react`/`react-dom` 19 · `react-router-dom` 7 · `@tanstack/react-query` 5
Tooling: Vite 8 · TypeScript ~6 · ESLint 10 (+ react-hooks, react-refresh) · Vitest 4 · Testing Library (React/DOM/jest-dom/user-event) · jsdom

## Internal Contracts (front ⟷ back)

| Frontend type (`types/index.ts`) | Backend DTO (`Dtos/`) |
|----------------------------------|------------------------|
| `Shop` | `ShopDto` |
| `User` | `UserDto` |
| `AuthResponse` | `AuthResponse` |
| `ShopFilters` (q/genre/area) | `GetShops` query params |

## Config & Secrets

| Key | Where | Notes |
|-----|-------|-------|
| `Jwt:Key/Issuer/Audience/ExpiryMinutes` | appsettings (`Jwt` section) | dev key in `appsettings.Development.json`; **rotate for prod** |
| `ConnectionStrings:DefaultConnection` | appsettings | falls back to `Data Source=ramensite.db` |
| `VITE_API_BASE_URL` | frontend env | defaults to `http://localhost:5105` |
| CORS origins | Program.cs | `localhost:5173`, `5174` |

## See Also

- [architecture.md](architecture.md) · [backend.md](backend.md)
