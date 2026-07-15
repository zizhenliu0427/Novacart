using System.IO.Compression;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Novacart.Api.Data;
using Novacart.Api.Models.Entities;
using Novacart.Api.Services;
using StackExchange.Redis;

using Novacart.Api.Services.Catalog;
using Novacart.Api.Services.Payments;
using Novacart.Api.Search;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// Initialize Stripe API Key
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

// ── Services ──────────────────────────────────────────────

// PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var config = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(config);
});

// Application services
builder.Services.AddSingleton<IRedisCacheService, RedisCacheService>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IProductService, Novacart.Api.Services.ProductService>();
builder.Services.AddNovacartElasticsearch(builder.Configuration);
builder.Services.AddScoped<IAdminProductService, AdminProductService>();
builder.Services.AddScoped<IAdminOrderService, AdminOrderService>();
builder.Services.AddScoped<IPriceRuleService, PriceRuleService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IProductCatalogStore, DbProductCatalogStore>();
builder.Services.Configure<Novacart.Api.Infrastructure.CartRedisOptions>(
    builder.Configuration.GetSection(Novacart.Api.Infrastructure.CartRedisOptions.SectionName));
builder.Services.AddSingleton<Novacart.Api.Services.CartRedis.ICartRedisStore>(sp =>
{
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Novacart.Api.Infrastructure.CartRedisOptions>>().Value;
    return opts.Enabled
        ? ActivatorUtilities.CreateInstance<Novacart.Api.Services.CartRedis.CartRedisStore>(sp)
        : Novacart.Api.Services.CartRedis.DisabledCartRedisStore.Instance;
});
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPaymentStrategy, StripePaymentStrategy>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// P3-3: Factory pattern (README #13)
builder.Services.AddScoped<Novacart.Api.Factories.IOrderFactory, Novacart.Api.Factories.OrderFactory>();
builder.Services.AddScoped<Novacart.Api.Factories.IPaymentStrategyFactory, Novacart.Api.Factories.PaymentStrategyFactory>();

// P2 services (fully implemented — see HANDOFF §7 / §13)
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IAddressService, AddressService>();
builder.Services.AddScoped<ISquareCatalogueGateway, SquareCatalogueGateway>();
builder.Services.AddScoped<ISquareCatalogueService, SquareCatalogueService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Exchange rates (Frankfurter API, Redis-cached)
builder.Services.AddHttpClient<ICurrencyService, CurrencyService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});

// Async email queue: producers enqueue, EmailBackgroundWorker drains and sends.
builder.Services.AddSingleton<EmailQueue>();
builder.Services.AddSingleton<IEmailQueue>(sp => sp.GetRequiredService<EmailQueue>());
builder.Services.AddHostedService<EmailBackgroundWorker>();

// S3 object storage (LocalStack in dev, real AWS in prod — config-driven).
builder.Services.AddSingleton<Novacart.Api.Storage.IS3StorageService, Novacart.Api.Storage.S3StorageService>();

// Response compression (Brotli + Gzip)
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);

// Required by [ResponseCache] attributes on controllers
builder.Services.AddResponseCaching();

// JWT authentication
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear(); // keep "sub"/"email" claim names intact
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "novacart",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "novacart",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };

        // Allow JWT to be read from the HttpOnly cookie as a fallback.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                ctx.Token ??= ctx.Request.Cookies["novacart_jwt"];
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

// Global exception handling — maps AppException/AuthException/etc. to ProblemDetails
// so controllers don't need try/catch. Registered before controllers.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Controllers + Swagger (with Bearer auth support)
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var problemDetails = new ValidationProblemDetails(context.ModelState)
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Please refer to the errors property for details.",
                Instance = context.HttpContext.Request.Path
            };
            return new BadRequestObjectResult(problemDetails);
        };
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Novacart API", Version = "v1" });
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT here (without the 'Bearer ' prefix).",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
    };
    options.AddSecurityDefinition("Bearer", scheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });
});

// CORS (allow Next.js frontend — local dev + configurable production origin)
var corsOrigins = new List<string> { "http://localhost:3000", "http://localhost:3001" };
var prodOrigin = builder.Configuration["Cors:AllowedOrigin"];
if (!string.IsNullOrEmpty(prodOrigin)) corsOrigins.Add(prodOrigin);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(corsOrigins.ToArray())
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

// Apply pending migrations on startup (Development convenience — no manual `dotnet ef` needed).
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
    {
        db.Database.Migrate();
    }
    else
    {
        db.Database.EnsureCreated();
    }

    // Dev-only admin bootstrap: ensures an admin account exists for exercising /api/admin/*.
    // Credentials come from configuration (see appsettings or env). NEVER enabled in production.
    await EnsureDevAdminAsync(db, builder.Configuration);
}

static async Task EnsureDevAdminAsync(AppDbContext db, IConfiguration config)
{
    var email = config["DevBootstrap:AdminEmail"] ?? "admin@novacart.local";
    var password = config["DevBootstrap:AdminPassword"] ?? "Admin123!";
    var fullName = config["DevBootstrap:AdminName"] ?? "Dev Admin";

    // Check if any admin already exists — skip if so.
    var adminRoleId = RoleNames.AdminId;
    var hasAdmin = db.Set<UserRole>().Any(ur => ur.RoleId == adminRoleId);
    if (hasAdmin) return;

    // Avoid duplicate email.
    if (db.Users.Any(u => u.Email == email)) return;

    db.Users.Add(new User
    {
        Email = email,
        FullName = fullName,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        IsActive = true,
    });
    await db.SaveChangesAsync();

    var user = db.Users.First(u => u.Email == email);
    db.Set<UserRole>().Add(new UserRole { UserId = user.Id, RoleId = adminRoleId });
    await db.SaveChangesAsync();
}

// ── Middleware ─────────────────────────────────────────────

// First in the pipeline — catches exceptions thrown by anything downstream
// and hands them to GlobalExceptionHandler for a clean ProblemDetails response.
app.UseExceptionHandler();

var cacheTypeName = app.Services.GetService<IRedisCacheService>()?.GetType().Name;
if (cacheTypeName != "NullRedisCacheService")
{
    app.UseResponseCompression();
    app.UseResponseCaching();
}
else
{
    app.Use(async (context, next) =>
    {
        context.Features.Set<Microsoft.AspNetCore.ResponseCaching.IResponseCachingFeature>(new DummyResponseCachingFeature());
        
        var originalBody = context.Response.Body;
        using var wrapper = new System.IO.MemoryStream();
        context.Response.Body = wrapper;

        await next();

        wrapper.Position = 0;
        await wrapper.CopyToAsync(originalBody);
    });
}


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint — probes DB; Redis is a non-fatal warning (cache is optional).
// IRedisCacheService is swapped for NullRedisCacheService in integration tests.
app.MapGet("/api/health", async (AppDbContext db, IRedisCacheService cache) =>
{
    bool dbOk;
    try { dbOk = await db.Database.CanConnectAsync(); }
    catch { dbOk = false; }

    bool redisOk;
    try
    {
        // Ping via a tiny cache round-trip. NullRedisCacheService always succeeds (no-op).
        await cache.SetAsync("__health__", "ok", TimeSpan.FromSeconds(5));
        var ping = await cache.GetAsync<string>("__health__");
        redisOk = ping is not null;
    }
    catch { redisOk = false; }

    var status = dbOk ? (redisOk ? "healthy" : "degraded") : "unhealthy";
    var result = new
    {
        status,
        timestamp = DateTime.UtcNow,
        environment = app.Environment.EnvironmentName,
        database = dbOk,
        redis = redisOk
    };

    // Only fail the health check if the database is unreachable.
    return dbOk ? Results.Ok(result) : Results.Json(result, statusCode: 503);
});

app.Run();

public partial class Program { }

public class DummyResponseCachingFeature : Microsoft.AspNetCore.ResponseCaching.IResponseCachingFeature
{
    public string[]? VaryByQueryKeys { get; set; }
}
