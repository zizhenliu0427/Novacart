using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var rateSection = builder.Configuration.GetSection("RateLimiting");
var checkoutPermit = rateSection.GetValue("CheckoutPermitLimit", 30);
var checkoutWindowSec = rateSection.GetValue("CheckoutWindowSeconds", 60);
var checkoutQueue = rateSection.GetValue("CheckoutQueueLimit", 10);
var defaultPermit = rateSection.GetValue("DefaultPermitLimit", 300);
var defaultWindowSec = rateSection.GetValue("DefaultWindowSeconds", 60);
var chatPermit = rateSection.GetValue("ChatPermitLimit", 10);
var chatWindowSec = rateSection.GetValue("ChatWindowSeconds", 60);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (path.Contains("/webhook/", StringComparison.OrdinalIgnoreCase))
            return RateLimitPartition.GetNoLimiter("webhook");

        var partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (path.StartsWith("/api/checkout", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                $"checkout:{partitionKey}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = checkoutPermit,
                    Window = TimeSpan.FromSeconds(checkoutWindowSec),
                    QueueLimit = checkoutQueue,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                });
        }

        if (path.StartsWith("/api/support", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                $"chat:{partitionKey}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = chatPermit,
                    Window = TimeSpan.FromSeconds(chatWindowSec),
                });
        }

        return RateLimitPartition.GetFixedWindowLimiter(
            $"default:{partitionKey}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = defaultPermit,
                Window = TimeSpan.FromSeconds(defaultWindowSec),
            });
    });

    options.OnRejected = async (context, token) =>
    {
        if (!context.HttpContext.Response.Headers.ContainsKey("Retry-After"))
            context.HttpContext.Response.Headers.RetryAfter = checkoutWindowSec.ToString();

        await context.HttpContext.Response.WriteAsync(
            "Too many requests. Please retry later.",
            cancellationToken: token);
    };
});

var app = builder.Build();
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UseRateLimiter();
app.MapDefaultEndpoints();
app.MapReverseProxy();
app.Run();

public partial class Program { }
