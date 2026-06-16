#!/usr/bin/env bash
#
# ラーメン図鑑 (ramen-guide) 開発用 起動スクリプト
#
# バックエンド (ASP.NET Core / :5105) とフロントエンド (Vite / :5173) を
# まとめて起動し、Ctrl+C で両方を確実に停止する。
#
# 使い方:
#   ./run.sh            両方を起動（既定）
#   ./run.sh backend    バックエンドのみ起動
#   ./run.sh frontend   フロントエンドのみ起動
#
set -euo pipefail

# スクリプトのある場所をリポジトリルートとして扱う。
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$ROOT_DIR/backend"
FRONTEND_DIR="$ROOT_DIR/frontend"

BACKEND_URL="http://localhost:5105"
FRONTEND_URL="http://localhost:5173"

TARGET="${1:-all}"

# 起動した子プロセスの PID を保持し、終了時にまとめて停止する。
PIDS=()

cleanup() {
  echo ""
  echo "==> 停止中..."
  for pid in "${PIDS[@]:-}"; do
    if [[ -n "$pid" ]] && kill -0 "$pid" 2>/dev/null; then
      # プロセスグループごと停止し、子（dotnet/node）も確実に終了させる。
      kill -TERM "$pid" 2>/dev/null || true
    fi
  done
  wait 2>/dev/null || true
  echo "==> 完了。"
}
trap cleanup EXIT INT TERM

require() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "エラー: '$1' が見つかりません。$2" >&2
    exit 1
  fi
}

start_backend() {
  require dotnet ".NET SDK 10 をインストールしてください。"
  echo "==> バックエンドを起動: $BACKEND_URL (Swagger: $BACKEND_URL/swagger)"
  (
    cd "$BACKEND_DIR"
    # Development 環境で起動（既定鍵・既定管理者資格情報を許可）。
    ASPNETCORE_ENVIRONMENT=Development dotnet run --launch-profile http
  ) &
  PIDS+=("$!")
}

start_frontend() {
  require npm "Node.js 20+ をインストールしてください。"
  if [[ ! -d "$FRONTEND_DIR/node_modules" ]]; then
    echo "==> 依存関係をインストール中 (npm install)..."
    (cd "$FRONTEND_DIR" && npm install)
  fi
  echo "==> フロントエンドを起動: $FRONTEND_URL"
  (
    cd "$FRONTEND_DIR"
    npm run dev
  ) &
  PIDS+=("$!")
}

case "$TARGET" in
  backend)  start_backend ;;
  frontend) start_frontend ;;
  all)      start_backend; start_frontend ;;
  *)
    echo "使い方: ./run.sh [all|backend|frontend]" >&2
    exit 1
    ;;
esac

echo ""
echo "==> 起動しました。停止するには Ctrl+C を押してください。"

# いずれかのプロセスが終了したらスクリプト全体を終了する（trap で後始末）。
wait -n 2>/dev/null || wait
