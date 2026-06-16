<!-- Generated: 2026-06-16 | Files scanned: 38 | Token estimate: ~550 -->

# System Architecture

ラーメン店紹介・検索 Web アプリ。2 層構成（SPA フロント + REST API バックエンド）。

## Topology

```
Browser (React SPA, :5173)
   │  fetch + JWT (Bearer)
   ▼
ASP.NET Core Web API (:5105)
   │  EF Core
   ▼
SQLite (ramensite.db)
```

## Service Boundaries

| Layer | Tech | Dir |
|-------|------|-----|
| Frontend | React 19 + TS, Vite, React Router 7, TanStack Query 5 | `frontend/` |
| Backend  | ASP.NET Core (.NET 10) Web API, controllers | `backend/` |
| Data     | EF Core + SQLite, code-first migrations + seed | `backend/Data/`, `backend/Migrations/` |
| Tests    | xUnit integration (WebApplicationFactory) | `tests/` + frontend Vitest/RTL |

## Data Flow (shop search)

```
ShopListPage → useQuery(['shops',filters]) → fetchShops()
  → GET /api/shops?q&genre&area → ShopsController.GetShops
  → EF Core LINQ filter on Shops → ShopDto[] → render ShopCard[]
```

## Auth Flow

```
LoginPage → useAuth().login → POST /api/auth/login
  → AuthController verifies PasswordHasher → TokenService.CreateToken (JWT)
  → token stored in localStorage → attached as Bearer on auth calls
  → AuthProvider restores session on boot via GET /api/auth/me
```

## Cross-Cutting

- **CORS**: API allows `localhost:5173/5174` (Vite dev).
- **AuthN**: JWT Bearer; signing key/issuer/audience from config (`Jwt` section).
- **Startup**: API auto-applies migrations + seeds 6 sample shops on boot.

## See Also

- [backend.md](backend.md) · [frontend.md](frontend.md) · [data.md](data.md) · [dependencies.md](dependencies.md)
