namespace Novacart.Api.Storage;

/// <summary>
/// Storage abstraction for object upload/download. Backed by S3 (or LocalStack in dev).
/// Keeping this narrow lets us mock it in tests and swap providers later.
/// </summary>
public interface IS3StorageService
{
    /// <summary>
    /// Generate a short-lived presigned PUT URL the client can use to upload directly to S3.
    /// </summary>
    /// <param name="objectKey">The key (path) within the bucket, e.g. <c>products/{guid}.jpg</c>.</param>
    /// <param name="contentType">Expected content type of the upload (e.g. <c>image/jpeg</c>).</param>
    Task<PresignedUrlResult> GeneratePresignedUploadUrlAsync(string objectKey, string contentType);

    /// <summary>
    /// The public (or presigned) URL for reading an object by its key. For a public-read
    /// bucket this is a stable URL; the implementation decides.
    /// </summary>
    Task<string> GetPublicUrlAsync(string objectKey);
}

public record PresignedUrlResult
{
    /// <summary>The presigned URL the client PUTs the file to.</summary>
    public required string UploadUrl { get; init; }

    /// <summary>The resulting object URL to store as the product's ImageUrl.</summary>
    public required string PublicUrl { get; init; }

    /// <summary>The object key within the bucket.</summary>
    public required string ObjectKey { get; init; }
}
