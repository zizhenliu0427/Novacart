using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Novacart.Api.Data;

var builder = WebApplication.CreateBuilder(args);

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

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS (allow Next.js frontend)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// ── Middleware ─────────────────────────────────────────────

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowFrontend");

app.MapControllers();

// Health check endpoint
app.MapGet("/api/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName
}));

app.Run();
