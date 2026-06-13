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
| POST | `/api/auth/register` | ユーザー登録 |
| POST | `/api/auth/login` | ログイン（JWT 発行） |
| GET | `/api/auth/me` | ログイン中ユーザー情報（要認証） |

## 設定 / セキュリティ

- `backend/appsettings.Development.json` の `Jwt:Key` は**ローカル開発用のプレースホルダ**です。本番では必ず環境変数（`Jwt__Key`）などで上書きしてください。
- `frontend/.env` の `VITE_API_BASE_URL` でバックエンドの URL を指定します。

## 今後の予定

- お気に入り登録、レビュー・星評価投稿
- 管理画面（店舗の追加・編集）
- UI デザインの作り込み
