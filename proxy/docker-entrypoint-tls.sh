#!/bin/sh
# プロキシ起動前に、TLS 証明書が無ければローカル開発用の自己署名証明書を生成する。
#
# 証明書は名前付き volume (/etc/nginx/certs) に保存され、コンテナ再作成後も保持される。
# 本番では実証明書（Let's Encrypt 等）をこのディレクトリにマウントして上書きすること
# （localhost.crt / localhost.key の名前に合わせるか、nginx.conf のパスを変更する）。
set -e

CERT_DIR=/etc/nginx/certs
CERT_FILE="$CERT_DIR/localhost.crt"
KEY_FILE="$CERT_DIR/localhost.key"

if [ ! -f "$CERT_FILE" ] || [ ! -f "$KEY_FILE" ]; then
  echo "[proxy] TLS 証明書が見つかりません。ローカル開発用の自己署名証明書を生成します..."
  mkdir -p "$CERT_DIR"
  openssl req -x509 -nodes -newkey rsa:2048 \
    -keyout "$KEY_FILE" \
    -out "$CERT_FILE" \
    -days 825 \
    -subj "/CN=localhost" \
    -addext "subjectAltName=DNS:localhost,IP:127.0.0.1"
  echo "[proxy] 自己署名証明書を生成しました（ブラウザでは警告が出ます）。"
fi

# 公式 nginx イメージの既定エントリポイントへ委譲（テンプレート処理等を維持）。
exec /docker-entrypoint.sh "$@"
