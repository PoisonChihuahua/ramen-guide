using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
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
builder.Services.AddSwaggerGen();

// SQLite + EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
                      ?? "Data Source=ramensite.db"));

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

// フロントエンド (Vite dev) からのアクセスを許可
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()); // httpOnly Cookie を跨いで送受信するために必須
});

var app = builder.Build();

// --- 起動時: マイグレーション適用 + シード ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    SeedData.Initialize(db);
}

// --- HTTP pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(FrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// 統合テスト（WebApplicationFactory<Program>）からエントリポイントを参照するために公開する。
public partial class Program { }
