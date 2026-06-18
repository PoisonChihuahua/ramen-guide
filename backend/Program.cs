using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RamenSite.Api.Data;
using RamenSite.Api.Models;
using RamenSite.Api.Services;
using RamenSite.Api.Services.Rag;

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

// RAG（自然文検索）。埋め込みと索引はステートレス/共有なので Singleton、
// DB を使う検索サービスはリクエストスコープ。生成は既定でキー不要のテンプレ実装。
builder.Services.AddSingleton<IEmbeddingService, SimpleEmbeddingService>();
builder.Services.AddSingleton<ShopEmbeddingIndex>();
builder.Services.AddSingleton<IAnswerGenerator, TemplateAnswerGenerator>();
builder.Services.AddScoped<RagSearchService>();

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                  ?? throw new InvalidOperationException("Jwt 設定が見つかりません。");

if (string.IsNullOrWhiteSpace(jwtSettings.Key))
{
    throw new InvalidOperationException("Jwt:Key が未設定です。appsettings または環境変数で設定してください。");
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
    });

builder.Services.AddAuthorization();

// フロントエンド (Vite dev) からのアクセスを許可
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// --- 起動時: マイグレーション適用 + シード ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    SeedData.Initialize(db);

    // ① Indexing: 全店舗を一度だけベクトル化してメモリ索引に載せる。
    var embedder = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
    var index = scope.ServiceProvider.GetRequiredService<ShopEmbeddingIndex>();
    index.Build(db.Shops.ToList(), embedder);
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
