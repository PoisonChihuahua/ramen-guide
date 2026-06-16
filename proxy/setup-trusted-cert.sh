#!/usr/bin/env bash
#
# ブラウザに「信頼される」ローカル TLS 証明書を mkcert で生成する。
#
# 自己署名証明書はブラウザで警告が出るが、mkcert はローカル CA を OS／ブラウザの信頼
# ストアに登録するため、その CA が署名した証明書は警告なしで受け入れられる。
#
# 使い方:
#   ./proxy/setup-trusted-cert.sh
#   docker compose restart proxy      # 生成後に反映
#
set -euo pipefail

CERT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/certs"

if ! command -v mkcert >/dev/null 2>&1; then
  echo "エラー: mkcert が見つかりません。インストールしてください:" >&2
  echo "  macOS:  brew install mkcert nss" >&2
  echo "  その他: https://github.com/FiloSottile/mkcert#installation" >&2
  exit 1
fi

# ローカル CA を OS／ブラウザの信頼ストアへ登録（初回のみ実体作成。冪等なので再実行可）。
# 実行時にキーチェーンへの登録で管理者パスワードを求められることがある。
echo "==> ローカル CA を信頼ストアへ登録 (mkcert -install)..."
mkcert -install

mkdir -p "$CERT_DIR"

# nginx.conf が参照するファイル名 (localhost.crt / localhost.key) に合わせて出力。
echo "==> localhost 向けの証明書を生成: $CERT_DIR"
mkcert \
  -cert-file "$CERT_DIR/localhost.crt" \
  -key-file "$CERT_DIR/localhost.key" \
  localhost 127.0.0.1 ::1

echo ""
echo "✓ 信頼される証明書を生成しました。"
echo "  反映するには: docker compose restart proxy"
echo "  その後 https://localhost を開くと証明書警告は出ません。"
