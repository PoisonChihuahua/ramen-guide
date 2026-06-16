# syntax=docker/dockerfile:1

# ラーメン図鑑 (ramen-guide) — リバースプロキシ（TLS 終端付き）
#
# 公式 nginx に openssl を足し、起動時に自己署名証明書を生成するエントリポイントを差し込む。
# ルーティング設定 (nginx.conf) は compose のバインドマウントで注入する（編集→restart で反映）。
FROM nginx:alpine

# 自己署名証明書の生成に openssl CLI が必要。
RUN apk add --no-cache openssl

COPY docker-entrypoint-tls.sh /docker-entrypoint-tls.sh
RUN chmod +x /docker-entrypoint-tls.sh

EXPOSE 80 443

ENTRYPOINT ["/docker-entrypoint-tls.sh"]
CMD ["nginx", "-g", "daemon off;"]
