<!-- Generated: 2026-06-16 | Files scanned: 19 | Token estimate: ~700 -->

# Frontend Architecture

React 19 + TypeScript SPA (Vite). Routing via React Router 7, server-state via TanStack Query, auth via React Context.

## Entry / Providers

```
main.tsx
  └ QueryClientProvider
     └ BrowserRouter
        └ AuthProvider          (context/AuthContext.tsx)
           └ App                (route table)
```

## Route Tree (App.tsx)

```
<Layout>                  (components/Layout.tsx — header/nav/footer, Outlet)
  index            → ShopListPage
  shops/:id        → ShopDetailPage
  login            → LoginPage
  register         → RegisterPage
```

## Component Hierarchy

```
ShopListPage
  ├ hero + SearchBar(filters,onChange)     ← live filter, controlled
  └ useQuery(['shops',filters]) → ShopCard[] (Link → /shops/:id)
ShopDetailPage → useQuery(['shop',id]) → fetchShop
LoginPage / RegisterPage → useAuth().login/register → navigate
```

## State Management

| Concern | Mechanism |
|---------|-----------|
| Server data (shops) | TanStack Query `useQuery`, keys `['shops',filters]` / `['shop',id]` |
| Auth/session | `AuthContext` + `useAuth()` hook; user persisted via JWT in localStorage |
| Search filters | local `useState<ShopFilters>` in ShopListPage |

## Auth Context (context/)

```
auth-context.ts   → createContext<AuthContextValue>  (user,isLoading,login,register,logout)
AuthContext.tsx   → AuthProvider: boot restore via fetchMe(), login/register/logout
hooks/useAuth.ts  → useContext guard (throws outside provider)
```

## API Layer (api/)

```
client.ts  → apiFetch<T>(path,{method,body,auth}) — base URL (VITE_API_BASE_URL ?? :5105),
             JWT Bearer injection, ApiError(status,message), token get/set/clear (localStorage)
shops.ts   → fetchShops(filters) / fetchShop(id)
auth.ts    → register / login / fetchMe
```

## Key Files

| File | Role |
|------|------|
| `components/Layout.tsx` | shell: header nav (login/logout), footer |
| `components/SearchBar.tsx` | keyword + genre + area filters (controlled) |
| `components/ShopCard.tsx` | shop tile, links to detail |
| `pages/ShopListPage.tsx` | hero + search + grid |
| `pages/ShopDetailPage.tsx` | single shop view |
| `pages/{Login,Register}Page.tsx` | auth forms |
| `types/index.ts` | `Shop`, `User`, `AuthResponse`, `ShopFilters` |

## Tests (Vitest + RTL)

`SearchBar.test.tsx`, `ShopCard.test.tsx`, `ShopListPage.test.tsx`; setup `test/setup.ts`.

## Constants / Config

- Genres: 醤油 / 味噌 / 豚骨 / 塩 / 豚骨醤油 · Areas: 札幌 / 東京 / 横浜 / 博多 (SearchBar.tsx)
- API base override: `VITE_API_BASE_URL`
- Token key: `ramensite_token` (localStorage)

## See Also

- [backend.md](backend.md) · [dependencies.md](dependencies.md)
