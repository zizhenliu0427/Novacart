namespace Novacart.Api.Services;

/// <summary>
/// Shared application exception that carries an HTTP status code.
/// Controllers catch this and call Problem(detail, statusCode) — keeps controllers thin.
/// </summary>
public class AppException : Exception
{
    public int StatusCode { get; }

    public AppException(string message, int statusCode = StatusCodes.Status400BadRequest)
        : base(message) => StatusCode = statusCode;

    public static AppException NotFound(string resource = "Resource") =>
        new($"{resource} not found.", StatusCodes.Status404NotFound);

    public static AppException Conflict(string detail) =>
        new(detail, StatusCodes.Status409Conflict);

    public static AppException Forbidden(string detail = "Access denied.") =>
        new(detail, StatusCodes.Status403Forbidden);

    /// <summary>Request is well-formed but semantically wrong (e.g. illegal state transition).</summary>
    public static AppException Unprocessable(string detail) =>
        new(detail, StatusCodes.Status422UnprocessableEntity);

    /// <summary>P2 scaffold: endpoint/service exists but its body isn't implemented yet.</summary>
    public static AppException NotImplemented(string detail = "Not implemented yet.") =>
        new(detail, StatusCodes.Status501NotImplemented);
}
