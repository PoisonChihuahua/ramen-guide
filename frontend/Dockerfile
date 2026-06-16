# syntax=docker/dockerfile:1

# ---- ビルドステージ: Vite で静的アセットを生成 ----
FROM node:24-alpine AS build
WORKDIR /app

# 依存解決のレイヤーキャッシュを効かせるため lockfile だけ先にコピー。
COPY package.json package-lock.json ./
RUN npm ci

COPY . ./

# API のベース URL はビルド時に焼き込まれる（Vite の import.meta.env）。
# 既定はホストから見たバックエンドの公開ポート。別ドメインへ配信する場合は
# compose / build 時に --build-arg VITE_API_BASE_URL=... で上書きする。
ARG VITE_API_BASE_URL=http://localhost:5105
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
RUN npm run build

# ---- 実行ステージ: nginx で静的配信 ----
FROM nginx:alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
