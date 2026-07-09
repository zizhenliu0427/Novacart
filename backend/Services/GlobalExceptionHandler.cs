using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Novacart.Api.Services;

/// <summary>
/// Central exception → HTTP mapping. Registered via <see cref="IExceptionHandler"/>
/// and invoked by <c>app.UseExceptionHandler()</c> so controllers never need try/catch.
/// </summary>
/// <remarks>
/// Maps the project's domain exceptions to consistent ProblemDetails responses:
/// <list type="bullet">
///   <item><see cref="AppException"/> / <see cref="AuthException"/> → their own status code.</item>
///   <item><see cref="UnauthorizedAccessException"/> → 401 (thrown by GetUserId() helpers).</item>
///   <item>Everything else → 500 (logged), with a generic message in production.</item>
/// </list>
/// </remarks>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            AppException appEx => (appEx.StatusCode, "Request failed"),
            AuthException authEx => (authEx.StatusCode, "Authentication failed"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            _ => (StatusCodes.Status500InternalServerError, "Server error"),
        };

        // Only log server-side faults at error level; expected 4xx are informational.
        if (statusCode >= 500)
            _logger.LogError(exception, "Unhandled exception on {Path}", httpContext.Request.Path);
        else
            _logger.LogInformation("Handled {Exception}: {Message}", exception.GetType().Name, exception.Message);

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = httpContext.Request.Path,
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
