# syntax=docker/dockerfile:1

# ---- ビルドステージ: SDK で publish ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 依存解決のレイヤーキャッシュを効かせるため csproj だけ先にコピーして restore。
COPY RamenSite.Api.csproj ./
RUN dotnet restore RamenSite.Api.csproj

# 残りのソースをコピーして Release publish。
COPY . ./
RUN dotnet publish RamenSite.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- 実行ステージ: ASP.NET ランタイムのみ ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# SQLite の DB ファイルを置く永続化ディレクトリ（compose で volume を割り当てる）。
# 非 root の app ユーザーが書き込めるよう所有者を変更しておく。
RUN mkdir -p /app/data && chown app:app /app/data

COPY --from=build /app/publish ./

# 非 root の既定ユーザー（aspnet イメージの app ユーザー）で実行。
USER app

# ASP.NET Core を 8080 で待ち受け（コンテナ標準ポート）。
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "RamenSite.Api.dll"]
