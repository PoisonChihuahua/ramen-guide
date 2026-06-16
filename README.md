# ラーメン図鑑 (ramen-guide)

全国のラーメン店を紹介・検索できる Web アプリ。バックエンド C#（ASP.NET Core）、フロントエンド React + TypeScript の2層構成。

## 機能

- 店舗一覧・詳細表示
- キーワード / ジャンル / エリアによる検索・絞り込み
- ユーザー登録・ログイン（JWT 認証の基盤）

## 技術スタック

| 層 | 技術 |
|----|------|
| バックエンド | ASP.NET Core (.NET 10) Web API, EF Core + SQLite, JWT 認証 |
| フロントエンド | React + TypeScript (Vite), React Router, TanStack Query |

## ディレクトリ構成

```
.
├── backend/    ASP.NET Core Web API (RamenSite.Api)
└── frontend/   Vite + React + TypeScript
```

## セットアップ

### 前提

- .NET SDK 10
- Node.js 20+

### バックエンド

```bash
cd backend
dotnet run
# http://localhost:5105 で起動（Swagger UI: http://localhost:5105/swagger）
```

起動時に SQLite DB のマイグレーション適用とサンプルデータ投入を自動で行います。

### フロントエンド

```bash
cd frontend
npm install
npm run dev
# http://localhost:5173 で起動
```

## 主な API

| メソッド | パス | 説明 |
|----------|------|------|
| GET | `/api/shops?q=&genre=&area=` | 店舗一覧（検索・絞り込み） |
| GET | `/api/shops/{id}` | 店舗詳細 |
| POST | `/api/auth/register` | ユーザー登録（認証 Cookie を発行） |
| POST | `/api/auth/login` | ログイン（認証 Cookie を発行） |
| POST | `/api/auth/logout` | ログアウト（認証 Cookie を破棄） |
| GET | `/api/auth/me` | ログイン中ユーザー情報（要認証） |

## 設定 / セキュリティ

- **JWT 署名鍵**: `backend/appsettings.Development.json` の `Jwt:Key` は**ローカル開発用のプレースホルダ**です。本番では必ず環境変数（`Jwt__Key`）などで上書きしてください。`Development` 以外の環境では、開発用の既定鍵のままや 32 文字未満の鍵だと**起動時に例外を投げて停止**します（弱い鍵での本番起動を防止）。
- **トークンの保存先**: JWT は `localStorage` ではなく **httpOnly Cookie**（`ramensite_auth`）に保存します。JavaScript から読み取れないため XSS によるトークン窃取を防ぎ、`SameSite=Strict` で CSRF を抑止、HTTPS 時は `Secure` 属性を付与します。フロントは `fetch` で `credentials: 'include'` を付けて Cookie を送受信します。
- **CORS**: Cookie を跨いで送受信するため、許可オリジン（`http://localhost:5173` / `5174`）に対して `AllowCredentials` を有効化しています。
- `frontend/.env` の `VITE_API_BASE_URL` でバックエンドの URL を指定します。

## 今後の予定

- お気に入り登録、レビュー・星評価投稿
- 管理画面（店舗の追加・編集）
- UI デザインの作り込み
