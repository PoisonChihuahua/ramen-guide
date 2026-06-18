# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

ラーメン図鑑 (ramen-guide): 全国のラーメン店を紹介・検索する Web アプリ。
2層構成 — `backend/` が ASP.NET Core (.NET 10) Web API、`frontend/` が Vite + React + TypeScript。
バックエンドとフロントは別プロセスで起動し、HTTP (CORS) 経由で通信する。

## Commands

### Backend (`backend/`)

```bash
dotnet run          # http://localhost:5105 で起動（Swagger: /swagger）
dotnet build
dotnet format       # フォーマット + アナライザ修正
```

起動時に `Program.cs` が SQLite のマイグレーション適用 (`db.Database.Migrate()`) と
サンプルデータ投入 (`SeedData.Initialize`) を自動実行する。DB ファイルは `backend/ramensite.db`。

### Backend テスト (`tests/RamenSite.Api.Tests/`)

```bash
cd tests/RamenSite.Api.Tests
dotnet test                                          # 全テスト（xUnit）
dotnet test --filter "FullyQualifiedName~ShopsApi"   # クラス単位
dotnet test --filter "DisplayName~GetShops_ReturnsAll"  # メソッド単位
```

ソリューションファイル (.sln) は無い。テストプロジェクトは `backend/RamenSite.Api.csproj` を直接 ProjectReference している。

### Frontend (`frontend/`)

```bash
npm install
npm run dev          # http://localhost:5173
npm run build        # tsc -b && vite build（型チェック込み）
npm run lint         # eslint
npm test             # vitest run（単発）
npm run test:watch   # vitest watch
npx vitest run src/components/ShopCard.test.tsx   # 単一ファイル
```

## Architecture

### Backend — レイヤー構成 (ASP.NET Core)

- **Controllers/** — `ShopsController`（一覧・詳細、`q`/`genre`/`area` でフィルタ）、`AuthController`（register/login/me）
- **Data/** — `AppDbContext`（EF Core, SQLite）と `SeedData`。`User.Email` は一意インデックス
- **Models/** — EF エンティティ (`Shop`, `User`)
- **Dtos/** — API 入出力の record 型。エンティティを直接返さず DTO に射影する
- **Services/** — `TokenService`（JWT 発行）と `JwtSettings`（Options パターン、`SectionName = "Jwt"`）
- **Migrations/** — EF Core マイグレーション

認証は ASP.NET Core 組み込みの JWT Bearer + `PasswordHasher<User>`。
`Program.cs` は起動時に `Jwt:Key` の存在を検証し、未設定なら例外で停止する。

### Frontend — データフローの要点

- **api/client.ts** — 共通 `apiFetch<T>` ラッパー。JWT 付与（`auth: true` 時）とエラーを `ApiError` に正規化。トークンは `localStorage`（キー `ramensite_token`）。ベース URL は `VITE_API_BASE_URL`（既定 `http://localhost:5105`）
- **api/auth.ts, api/shops.ts** — エンドポイント別の薄いラッパー。`apiFetch` 経由でのみ通信する
- **context/** — `AuthContext.tsx`（Provider 実装）と `auth-context.ts`（Context オブジェクト）を**意図的に分離**（react-refresh 対応）。消費は `hooks/useAuth.ts`
- **App.tsx** — React Router。全ルートが `Layout` の子。`/`, `/shops/:id`, `/login`, `/register`
- サーバー状態は TanStack Query で扱う方針

新しい API 呼び出しを足すときは `fetch` を直接書かず、必ず `api/` 配下に関数を追加して `apiFetch` を通すこと。

### RAG（自然文検索）— 学習用に実装

`POST /api/shops/ask` は埋め込みベースの RAG パイプライン。`backend/Services/Rag/` に集約:

- **`IEmbeddingService` / `SimpleEmbeddingService`** — テキスト→ベクトル。簡易版は外部APIキー不要（文字n-gram + ハッシュ + L2正規化）。**差し替え口**であり、本物の意味検索には Voyage/OpenAI 等の実装に交換する
- **`ShopEmbeddingIndex`**（Singleton）— 全店舗のベクトルをメモリ保持。`Program.cs` の起動ブロックで Seed 後に一度だけ `Build` する（①Indexing）
- **`RagSearchService`** — 質問をベクトル化→全店コサイン類似度→上位K（②Retrieval）→生成（③）
- **`IAnswerGenerator` / `TemplateAnswerGenerator`** — 既定はキー不要のテンプレ生成。`TemplateAnswerGenerator` のコメントに Claude（Anthropic C# SDK, `claude-opus-4-8`）への差し替え手順を記載

フロントは `api/rag.ts` ＋ `pages/AskPage.tsx`（`/ask` ルート）。`VectorMath`・`SimpleEmbeddingService` は単体テスト、エンドポイントは `AskApiTests` で統合テスト済み。

### 共有データモデル

`Shop` のフィールドはバック・フロントで一致させる:
`name / description / address / area / genre / openingHours / priceRange / imageUrl`
ジャンル例: 醤油・味噌・豚骨・塩・豚骨醤油 / エリア例: 札幌・東京・横浜・博多

## テスト戦略

- **Backend**: `RamenApiFactory`（`WebApplicationFactory<Program>`）でアプリを起動し HTTP 経由で統合テスト。
  本番 SQLite ファイルではなく、接続を開いたままのインメモリ SQLite に差し替える。テスト用 JWT 設定もここで注入。
  この仕組みのため `Program.cs` 末尾に `public partial class Program { }` があり、削除してはいけない。
- **Frontend**: Vitest + React Testing Library（jsdom）。`src/test/setup.ts` が共通セットアップ。テストはソースと同階層に `*.test.tsx`。

## 設定・セキュリティ

- `backend/appsettings.Development.json` の `Jwt:Key` はローカル開発用プレースホルダ。本番は環境変数 `Jwt__Key` で上書きする
- CORS は `Program.cs` の `FrontendCorsPolicy` で `localhost:5173`/`5174` を許可（フロントのポートを変えたらここも更新）
