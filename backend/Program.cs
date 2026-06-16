using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RamenSite.Api.Data;
using RamenSite.Api.Models;
using RamenSite.Api.Services;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicy = "FrontendCors";

// --- Services ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

// リバースプロキシ経由のヘッダを信頼する。プロキシが付与する X-Forwarded-For /
// X-Forwarded-Proto を解釈し、実クライアント IP（レート制限のパーティションキー）と
// スキーム（Cookie の Secure 判定）を復元する。backend はホストへ公開せず Docker 内部
// ネットワークのプロキシからのみ到達するため、既定のネットワーク制限は解除して信頼する。
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// PostgreSQL + EF Core。接続文字列は ConnectionStrings__DefaultConnection（環境変数）から注入する。
// 本番では InsForge マネージド Postgres の接続文字列を Compute の env/secret で渡す。
// 統合テストはこの登録を取り除き、インメモリ SQLite に差し替える（RamenApiFactory 参照）。
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")
                      ?? throw new InvalidOperationException(
                          "ConnectionStrings:DefaultConnection が未設定です。環境変数で Postgres 接続文字列を設定してください。")));

// パスワードハッシュ（ASP.NET Core 組み込み）
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

// JWT 設定（Options パターン）
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddScoped<TokenService>();

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                  ?? throw new InvalidOperationException("Jwt 設定が見つかりません。");

if (string.IsNullOrWhiteSpace(jwtSettings.Key))
{
    throw new InvalidOperationException("Jwt:Key が未設定です。appsettings または環境変数で設定してください。");
}

// 本番安全ガード: 開発用のプレースホルダ鍵や短すぎる鍵での起動を拒否する。
// 本番では必ず環境変数 Jwt__Key などで安全な署名鍵に上書きすること。
const string DevPlaceholderJwtKey = "dev-only-signing-key-change-me-in-production-please-32chars+";
const int MinJwtKeyLength = 32;
if (!builder.Environment.IsDevelopment())
{
    if (jwtSettings.Key == DevPlaceholderJwtKey)
    {
        throw new InvalidOperationException(
            "本番環境で開発用の既定 Jwt:Key が使用されています。環境変数 Jwt__Key で安全な署名鍵を設定してください。");
    }

    if (jwtSettings.Key.Length < MinJwtKeyLength)
    {
        throw new InvalidOperationException(
            $"Jwt:Key は {MinJwtKeyLength} 文字以上にしてください。");
    }
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
        };

        // トークンは httpOnly Cookie に格納する。Authorization ヘッダが無い場合のみ
        // Cookie から読み取る（API ツールや統合テストのヘッダ方式も引き続き許可）。
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (!context.Request.Headers.ContainsKey("Authorization") &&
                    context.Request.Cookies.TryGetValue(AuthCookie.Name, out var cookieToken))
                {
                    context.Token = cookieToken;
                }

                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization();

// --- レート制限 ---
var rateLimitSettings = builder.Configuration.GetSection(RateLimitSettings.SectionName).Get<RateLimitSettings>()
                        ?? new RateLimitSettings();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // IP 単位のグローバル上限。
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitSettings.GlobalPermitLimit,
                Window = TimeSpan.FromSeconds(rateLimitSettings.WindowSeconds),
                QueueLimit = 0,
            }));

    // 認証エンドポイント向けの厳しめポリシー（IP 単位）。
    options.AddPolicy(RateLimitPolicies.Auth, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitSettings.AuthPermitLimit,
                Window = TimeSpan.FromSeconds(rateLimitSettings.WindowSeconds),
                QueueLimit = 0,
            }));
});

// フロントエンドからのアクセスを許可。許可 Origin は設定（Cors:AllowedOrigins）から注入し、
// 本番では環境変数 Cors__AllowedOrigins__0 などで本番ドメインを明示する。
// 未設定時はローカル開発用の Vite dev サーバーにフォールバックする。
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (corsOrigins is null || corsOrigins.Length == 0)
{
    corsOrigins = ["http://localhost:5173", "http://localhost:5174"];
}

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()); // httpOnly Cookie を跨いで送受信するために必須
});

var app = builder.Build();

// --- 起動時: マイグレーション適用 + シード ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // 本番（Npgsql）はマイグレーションを適用してスキーマをバージョン管理する。
    // 統合テストは SQLite（リレーショナルだが Npgsql 用マイグレーション SQL は適用できない）に
    // 差し替わるため、モデルから直接スキーマを生成する EnsureCreated にフォールバックする。
    if (db.Database.IsNpgsql())
    {
        db.Database.Migrate();
    }
    else
    {
        db.Database.EnsureCreated();
    }

    SeedData.Initialize(db, app.Environment.IsDevelopment());
}

// --- HTTP pipeline ---
// プロキシのヘッダ復元は他のミドルウェアより前に実行する必要がある。
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(FrontendCorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// 統合テスト（WebApplicationFactory<Program>）からエントリポイントを参照するために公開する。
public partial class Program { }
