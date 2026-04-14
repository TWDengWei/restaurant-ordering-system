using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RestaurantApi.Data;
using RestaurantApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── 資料庫 ──────────────────────────────────────────────
// 優先從 Render 注入的個別 DB 環境變數組裝，回退到 appsettings
var dbHost     = Environment.GetEnvironmentVariable("DB_HOST");
var dbPort     = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
var dbName     = Environment.GetEnvironmentVariable("DB_NAME");
var dbUser     = Environment.GetEnvironmentVariable("DB_USER");
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

var connectionString = (dbHost != null && dbName != null && dbUser != null && dbPassword != null)
    ? $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword};SSL Mode=Require;Trust Server Certificate=true"
    : builder.Configuration.GetConnectionString("DefaultConnection")!;

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
        npgsqlOptions.EnableRetryOnFailure(3)));

// ── JWT 驗證 ─────────────────────────────────────────────
var jwtSecret = builder.Configuration["JwtSettings:Secret"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── 上傳大小限制（圖片最大 10MB）─────────────────────────
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 10 * 1024 * 1024);

// ── CORS（開發用，允許所有來源）──────────────────────────
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// ── 初始化資料庫 ─────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.EnsureCreated();

        // Seed 管理員帳號（預設 admin / admin123）
        if (!db.Admins.Any())
        {
            db.Admins.Add(new Admin
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123")
            });
            db.SaveChanges();
        }

        // Seed 10 張桌位
        if (!db.Tables.Any())
        {
            db.Tables.AddRange(Enumerable.Range(1, 10)
                .Select(i => new Table { TableNumber = i, Name = $"桌 {i}" }));
            db.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "資料庫初始化失敗");
    }
}

// ── Middleware 管線 ──────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 全域錯誤處理（回傳 JSON 錯誤訊息，方便除錯）
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var msg = feature?.Error?.Message ?? "Internal server error";
        await context.Response.WriteAsync(
            System.Text.Json.JsonSerializer.Serialize(new { error = msg }));
    });
});

// 健康檢查端點（不需要 DB）
app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }));

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// 讓 /admin 直接導向 /admin/index.html
app.MapGet("/admin", () => Results.Redirect("/admin/index.html"));

app.Run();
