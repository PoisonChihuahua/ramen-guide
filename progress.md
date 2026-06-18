# 開発記録 (progress.md)

## 2026-06-19 — RAG（自然文検索）の学習用導入

### 目的
「RAGとは何かを手を動かして理解する」ことを目的に、題材「自然文での店舗検索」で
RAGパイプライン（Retrieval-Augmented Generation）を実装した。検索（Retrieval）の
中身がコードで見える構成を優先し、ベクトルDBは使わず素朴な全件コサイン計算にした。

### RAGの3工程と実装の対応
```
① Indexing   ShopEmbeddingIndex      … 起動時に全店舗をベクトル化してメモリ保持
② Retrieval  RagSearchService        … 質問をベクトル化→全店コサイン類似度→上位K件
③ Generation TemplateAnswerGenerator … 引いた店舗だけを根拠に回答を生成
```

### 追加・変更したファイル

**バックエンド (`backend/`)**
- `Services/Rag/IEmbeddingService.cs` — テキスト→ベクトルの差し替え可能インターフェース
- `Services/Rag/SimpleEmbeddingService.cs` — キー不要の簡易埋め込み（文字n-gram→FNV-1aハッシュ→L2正規化、512次元）
- `Services/Rag/VectorMath.cs` — コサイン類似度
- `Services/Rag/ShopEmbeddingIndex.cs` — 全店舗ベクトルのメモリ索引（Singleton）
- `Services/Rag/RagSearchService.cs` — 検索（②）＋生成呼び出し（③）
- `Services/Rag/IAnswerGenerator.cs` / `TemplateAnswerGenerator.cs` — 既定はキー不要テンプレ生成。Claude差し替え手順をコメントに記載
- `Dtos/RagDtos.cs` — `AskRequest` / `ShopMatchDto` / `AskResponse`
- `Controllers/AskController.cs` — `POST /api/shops/ask`（既存 `GET /api/shops` は無改変）
- `Program.cs` — RAGサービスのDI登録、起動時に索引を構築（①）

**フロントエンド (`frontend/src/`)**
- `api/rag.ts` — `askShops()`（apiFetch経由）
- `pages/AskPage.tsx` — 質問フォーム＋回答＋関連店舗カード
- `App.tsx` — `/ask` ルート追加
- `components/Layout.tsx` — ヘッダーに「AI検索」リンク追加
- `types/index.ts` — `ShopMatch` / `AskResponse` 型追加

**テスト**
- `tests/RamenSite.Api.Tests/Rag/VectorMathTests.cs` — コサイン類似度の性質
- `tests/RamenSite.Api.Tests/Rag/SimpleEmbeddingServiceTests.cs` — 決定性・正規化・関連度
- `tests/RamenSite.Api.Tests/AskApiTests.cs` — エンドポイント統合テスト
- `frontend/src/pages/AskPage.test.tsx` — 送信→回答/店舗表示、空入力でボタン無効

**ドキュメント**
- `CLAUDE.md` — RAGサブシステムの説明を追記

### 検証結果
- バックエンド: `dotnet test` → 32件すべて合格（「味噌のラーメンを教えて」→味噌店が最上位を統合テストで確認）
- フロント: `npm test` → 12件すべて合格、`tsc -b` クリーン
- 既知: `npm run lint` は `AuthContext.tsx:14` で1件赤。**今回の変更外の既存指摘**（新規ファイルはlintクリーン）

### 設計上の学び・限界
- **埋め込みは差し替え可能**にした（`IEmbeddingService`）。これがRAGの一番の学びどころ。
- 簡易埋め込みは「文字の重なり」しか見ないため、「こってり≈濃厚」のような同義表現の
  意味検索は限定的。本物の意味検索には学習済み埋め込みモデル（Voyage/OpenAI等、要APIキー）へ交換する。
- 生成（③）も `IAnswerGenerator` で差し替え可能。既定はキー不要のテンプレ生成。
  Claude（Anthropic C# SDK, `claude-opus-4-8`）への差し替え手順は
  `TemplateAnswerGenerator.cs` のコメントに記載。Anthropicは生成専用で埋め込みAPIを持たない点に注意。

### 次の差し替えポイント
| やりたいこと | 交換する場所 |
|------------|------------|
| 意味検索の精度向上 | `IEmbeddingService` を実埋め込みモデル実装に交換 |
| 本物のLLM生成 | `IAnswerGenerator` を Claude 実装に交換（`dotnet add package Anthropic`） |

### 動かし方
- バックエンド: `cd backend && dotnet run`
- フロント: `cd frontend && npm run dev` → ヘッダー「AI検索」または `/ask`
