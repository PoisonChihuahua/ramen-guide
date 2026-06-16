# デプロイ（InsForge 自動デプロイ）

`main` ブランチに push / マージされると、GitHub Actions
（[`.github/workflows/deploy-insforge.yml`](../.github/workflows/deploy-insforge.yml)）が
自動で InsForge へデプロイする。

## パイプライン構成

| ジョブ | 内容 |
| --- | --- |
| `verify` | フロント（Vite ビルド + Vitest）とバックエンド（`dotnet test`）を実行。失敗するとデプロイしない。 |
| `deploy-backend` | InsForge **Compute**（Fly.io / source モード）へ `backend/Dockerfile` をリモートビルドしてデプロイ（サービス名 `ramen-api` / port 8080）。DB マイグレーションは起動時に `Program.cs` が自動適用。 |
| `deploy-frontend` | InsForge **Deployments**（Vercel）へ `frontend/` をデプロイ。API のベース URL を build-time env で注入。 |

`verify` が通った後に backend / frontend を並列デプロイする。

## 初回セットアップ：GitHub の Secrets / Variables を登録する

CI からデプロイするには、リポジトリの
**Settings → Secrets and variables → Actions** に以下を登録する。
（値はリポジトリにコミットせず、必ず GitHub 側に保存する）

### Secrets（機微情報）

| Secret 名 | 説明 | 取得方法 |
| --- | --- | --- |
| `INSFORGE_EMAIL` | InsForge アカウントのメール | ログインに使うメール |
| `INSFORGE_PASSWORD` | InsForge アカウントのパスワード | 同上 |
| `INSFORGE_PROJECT_ID` | リンク対象プロジェクト ID | `npx @insforge/cli list`（appkey `8axk6wry` /「My First Project」の `project_id`）。※ramen-guide の `.insforge/project.json` はアーカイブ済みワークスペースと共に消失しているため CLI で取得する |
| `INSFORGE_ORG_ID` | 組織 ID | `npx @insforge/cli list --json` の org `id`（`~/.insforge/config.json` の `default_org_id` でも確認可） |
| `BACKEND_DB_CONNECTION_STRING` | 本番 Postgres の接続文字列 | 下記 env ファイルの `CONNECTIONSTRINGS__DEFAULTCONNECTION`、または `npx @insforge/cli db connection-string` |
| `BACKEND_JWT_KEY` | JWT 署名鍵（32文字以上） | 下記 env ファイルの `JWT__KEY` |
| `SEED_ADMIN_EMAIL` | 管理者シードのメール | 下記 env ファイルの `SEED_ADMIN_EMAIL` |
| `SEED_ADMIN_PASSWORD` | 管理者シードのパスワード | 下記 env ファイルの `SEED_ADMIN_PASSWORD`（未設定だと本番起動が失敗・必須） |

> **既存の本番秘密の在処**: 前回デプロイ時の env ファイルが
> `~/conductor/archived-contexts/ramen-guide/favicon-ramen-bowl/backend.env.production`
> に残っている（DSN / `JWT__KEY` / `SEED_ADMIN_*` を含む）。
> JWT の `Issuer=RamenSite` / `Audience=RamenSiteClient` は非機微の固定値で、ワークフロー内に直書きしている。

### Variables（非機微・URL）

| Variable 名 | 値 |
| --- | --- |
| `BACKEND_PUBLIC_URL` | `https://ramen-api-bca6294b-963c-4376-b572-6e7360509803.fly.dev` |
| `FRONTEND_PUBLIC_URL` | `https://8axk6wry.insforge.site` |

> `FRONTEND_PUBLIC_URL` はバックエンドの CORS 許可 Origin（`Cors__AllowedOrigins__0`）として、
> `BACKEND_PUBLIC_URL` はフロントの `VITE_API_BASE_URL` として使われる。

## 動作確認

1. 上記の Secrets / Variables をすべて登録する。
2. `main` に push（または PR をマージ）する。
3. GitHub の **Actions** タブで `Deploy to InsForge` の実行を確認する。
4. 手動実行したい場合は Actions タブから **Run workflow**（`workflow_dispatch`）。

## 補足・落とし穴

- Compute は source モードのため、CI で `flyctl` をセットアップしている（Docker デーモンは不要）。
- `--env-file` のキーは**全大文字必須**（`CONNECTIONSTRINGS__DEFAULTCONNECTION` など）。ASP.NET の Config は大小無視で解決する。
- フロント／バックエンドが別ドメインのため Cookie は `SameSite=None;Secure`。CORS 許可 Origin を本番ドメインに限定すること。
- `concurrency` 設定により、デプロイは同時に1つしか走らない（前のデプロイ完了を待つ）。
