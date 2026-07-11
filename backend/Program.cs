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

using Novacart.Api.Services.Payments;
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
builder.Services.AddScoped<IProductService, Novacart.Api.Services.ProductService>();
builder.Services.AddScoped<IAdminProductService, AdminProductService>();
builder.Services.AddScoped<IAdminOrderService, AdminOrderService>();
builder.Services.AddScoped<IPriceRuleService, PriceRuleService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPaymentStrategy, StripePaymentStrategy>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// P3-3: Factory pattern (README #13)
builder.Services.AddScoped<Novacart.Api.Factories.IOrderFactory, Novacart.Api.Factories.OrderFactory>();
builder.Services.AddScoped<Novacart.Api.Factories.IPaymentStrategyFactory, Novacart.Api.Factories.PaymentStrategyFactory>();

// P2 scaffold services (stub bodies — see HANDOFF §7 / §13)
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IAddressService, AddressService>();
builder.Services.AddScoped<ISquareCatalogueGateway, SquareCatalogueGateway>();
builder.Services.AddScoped<ISquareCatalogueService, SquareCatalogueService>();
builder.Services.AddScoped<IEmailService, EmailService>();

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

// CORS (allow Next.js frontend)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001")
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
    var adminRoleId = 2; // RoleNames.AdminId (seeded)
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

app.UseResponseCompression();
app.UseResponseCaching();


app.UseSwagger();
app.UseSwaggerUI();

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
