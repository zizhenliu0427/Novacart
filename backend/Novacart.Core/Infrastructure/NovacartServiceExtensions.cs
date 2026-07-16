using System.IO.Compression;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Novacart.Api.Data;
using Novacart.Api.Factories;
using Novacart.Api.Models.Entities;
using Novacart.Api.Services;
using Novacart.Api.Services.Catalog;
using Novacart.Api.Services.Orders;
using Novacart.Api.Services.Payments;
using Novacart.Api.Services.Stock;
using Novacart.Api.Services.CartRedis;
using Novacart.Api.Infrastructure.Messaging;
using Novacart.Api.Search;
using Novacart.Api.Storage;
using Novacart.Api.Infrastructure.Threading;
using StackExchange.Redis;
using Stripe;

namespace Novacart.Api.Infrastructure;

public static class NovacartServiceExtensions
{
    public static IServiceCollection AddNovacartInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];

        services.Configure<MicroservicesOptions>(configuration.GetSection(MicroservicesOptions.SectionName));

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var redis = configuration["Redis:Configuration"]
                ?? configuration.GetConnectionString("Redis")
                ?? "localhost:6379";
            return ConnectionMultiplexer.Connect(redis);
        });

        services.AddSingleton<IRedisCacheService, RedisCacheService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPricingService, PricingService>();
        services.AddHttpContextAccessor();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        services.AddResponseCompression(o =>
        {
            o.EnableForHttps = true;
            o.Providers.Add<BrotliCompressionProvider>();
            o.Providers.Add<GzipCompressionProvider>();
        });
        services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
        services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
        services.AddResponseCaching();

        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        var jwtSecret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"] ?? "novacart",
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"] ?? "novacart",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        ctx.Token ??= ctx.Request.Cookies["novacart_jwt"];
                        return Task.CompletedTask;
                    },
                };
            });
        services.AddAuthorization();
        services.AddControllers().ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var problemDetails = new ValidationProblemDetails(context.ModelState)
                {
                    Title = "One or more validation errors occurred.",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = context.HttpContext.Request.Path,
                };
                return new BadRequestObjectResult(problemDetails);
            };
        });

        var corsOrigins = new List<string> { "http://localhost:3000", "http://localhost:3001" };
        var prodOrigin = configuration["Cors:AllowedOrigin"];
        if (!string.IsNullOrEmpty(prodOrigin)) corsOrigins.Add(prodOrigin);
        services.AddCors(o => o.AddPolicy("AllowFrontend", p =>
            p.WithOrigins(corsOrigins.ToArray()).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

        return services;
    }

    public static IServiceCollection AddNovacartAuth(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IUserService, UserService>();
        return services;
    }

    public static IServiceCollection AddNovacartProduct(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddNovacartElasticsearch(configuration);
        services.Configure<StockHoldOptions>(configuration.GetSection(StockHoldOptions.SectionName));
        services.AddSingleton<IRedisDistributedLockService, RedisDistributedLockService>();
        services.AddScoped<IProductStockRepository, ProductStockRepository>();
        services.AddScoped<IStockHoldService, StockHoldService>();
        services.AddScoped<IStockReservationService, StockReservationService>();
        services.AddHostedService<StockHoldExpiryHostedService>();
        services.AddScoped<IProductService, Novacart.Api.Services.ProductService>();
        services.AddScoped<IAdminProductService, AdminProductService>();
        services.AddScoped<IPriceRuleService, PriceRuleService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<ISquareCatalogueGateway, SquareCatalogueGateway>();
        services.AddScoped<ISquareCatalogueService, SquareCatalogueService>();
        services.AddScoped<ICurrencyService, CurrencyService>();
        services.AddHttpClient<ICurrencyService, CurrencyService>(c => c.Timeout = TimeSpan.FromSeconds(15));
        services.AddSingleton<IS3StorageService, S3StorageService>();
        return services;
    }

    public static IServiceCollection AddNovacartCart(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CartRedisOptions>(configuration.GetSection(CartRedisOptions.SectionName));
        services.AddSingleton<ICartRedisStore>(sp =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CartRedisOptions>>().Value;
            return opts.Enabled
                ? ActivatorUtilities.CreateInstance<CartRedisStore>(sp)
                : DisabledCartRedisStore.Instance;
        });
        services.AddNovacartCatalogSupport(configuration);
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IWishlistService, WishlistService>();
        return services;
    }

    public static IServiceCollection AddNovacartOrder(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IAdminOrderService, AdminOrderService>();
        services.AddScoped<IAddressService, AddressService>();
        services.AddScoped<IPaymentStrategy, StripePaymentStrategy>();
        services.AddScoped<IStripeRefundService, StripeRefundService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IOrderFactory, OrderFactory>();
        services.AddScoped<IPaymentStrategyFactory, PaymentStrategyFactory>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddSingleton<EmailQueue>();
        services.AddSingleton<IEmailQueue>(sp => sp.GetRequiredService<EmailQueue>());
        services.AddHostedService<EmailBackgroundWorker>();
        services.AddScoped<IStockHoldGateway, LocalStockHoldGateway>();
        services.AddNovacartStripeWebhookHotPath(configuration);
        return services;
    }

    public static IServiceCollection AddNovacartOrderForMicroservice(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IAdminOrderService, AdminOrderService>();
        services.AddScoped<IAddressService, AddressService>();
        services.AddScoped<IPaymentStrategy, StripePaymentStrategy>();
        services.AddScoped<IStripeRefundService, StripeRefundService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IOrderFactory, OrderFactory>();
        services.AddScoped<IPaymentStrategyFactory, PaymentStrategyFactory>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddNovacartCatalogSupport(configuration);
        services.AddNovacartStockHoldGateway(configuration);
        services.AddScoped<IOrderCheckoutCompletionService, OrderCheckoutCompletionService>();
        services.AddScoped<ICheckoutSagaAdminService, CheckoutSagaAdminService>();
        services.AddScoped<IEmailQueue, MassTransitEmailQueue>();
        services.AddNovacartRabbitMqMonitoring();
        services.AddNovacartStripeWebhookHotPath(configuration);
        return services;
    }

    public static async Task MigrateAndSeedAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return;
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (db.Database.IsRelational()) db.Database.Migrate();
        else db.Database.EnsureCreated();
        await EnsureDevAdminAsync(db, app.Configuration);
    }

    private static async Task EnsureDevAdminAsync(AppDbContext db, IConfiguration config)
    {
        var email = config["DevBootstrap:AdminEmail"] ?? "admin@novacart.local";
        if (db.Set<UserRole>().Any(ur => ur.RoleId == RoleNames.AdminId)) return;
        if (db.Users.Any(u => u.Email == email)) return;
        db.Users.Add(new User
        {
            Email = email,
            FullName = config["DevBootstrap:AdminName"] ?? "Dev Admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(config["DevBootstrap:AdminPassword"] ?? "Admin123!"),
            IsActive = true,
        });
        await db.SaveChangesAsync();
        var user = db.Users.First(u => u.Email == email);
        db.Set<UserRole>().Add(new UserRole { UserId = user.Id, RoleId = RoleNames.AdminId });
        await db.SaveChangesAsync();
    }

    public static void MapNovacartHealth(this WebApplication app)
    {
        app.MapGet("/api/health", async (AppDbContext db, IRedisCacheService cache) =>
        {
            var dbOk = await db.Database.CanConnectAsync();
            var redisOk = false;
            try
            {
                await cache.SetAsync("__health__", "ok", TimeSpan.FromSeconds(5));
                redisOk = await cache.GetAsync<string>("__health__") is not null;
            }
            catch { /* optional */ }

            var result = new
            {
                status = dbOk ? "healthy" : "unhealthy",
                timestamp = DateTime.UtcNow,
                service = app.Environment.ApplicationName,
                database = dbOk,
                redis = redisOk,
            };
            return dbOk ? Results.Ok(result) : Results.Json(result, statusCode: 503);
        });
    }
}
