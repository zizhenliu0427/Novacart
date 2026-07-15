using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;

namespace Novacart.Api.Storage;

/// <summary>
/// S3-backed object storage. Reads endpoint/bucket/credentials from configuration so the same
/// code works against LocalStack in development and real AWS in production — set
/// <c>Aws:S3:ServiceUrl</c> to point at LocalStack, or leave it unset for real AWS.
/// </summary>
public class S3StorageService : IS3StorageService
{
    private readonly IAmazonS3 _client;
    private readonly string _bucketName;
    private readonly string _publicBaseUrl; // null/empty when using presigned GET URLs
    private readonly bool _forceHttp; // LocalStack over plain HTTP

    public S3StorageService(IConfiguration config)
    {
        _bucketName = config["Aws:S3:Bucket"]
            ?? throw new InvalidOperationException("Aws:S3:Bucket is not configured.");

        var region = config["Aws:S3:Region"] ?? "us-east-1";
        var serviceUrl = config["Aws:S3:ServiceUrl"];
        _forceHttp = !string.IsNullOrEmpty(serviceUrl)
                     && serviceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase);

        // When ServiceUrl is set, we're targeting LocalStack (or an S3-compatible store).
        if (!string.IsNullOrEmpty(serviceUrl))
        {
            var cfg = new AmazonS3Config
            {
                ServiceURL = serviceUrl,
                ForcePathStyle = bool.TryParse(config["Aws:S3:ForcePathStyle"], out var fps) ? fps : true,
                UseHttp = _forceHttp,
                AuthenticationRegion = region,
            };
            var accessKey = config["Aws:S3:AccessKey"] ?? "test";
            var secretKey = config["Aws:S3:SecretKey"] ?? "test";
            _client = new AmazonS3Client(accessKey, secretKey, cfg);
        }
        else
        {
            // Real AWS — use the default credential chain (env vars, IAM role, etc.).
            _client = new AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(region));
        }

        // Optional: a stable public URL prefix if the bucket is served over HTTP/CDN.
        _publicBaseUrl = config["Aws:S3:PublicBaseUrl"] ?? "";
    }

    public async Task<PresignedUrlResult> GeneratePresignedUploadUrlAsync(string objectKey, string contentType)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(5),
            ContentType = contentType,
        };

        var uploadUrl = await _client.GetPreSignedURLAsync(request);
        // LocalStack serves plain HTTP; the SDK may still emit https in the presigned URL.
        if (_forceHttp) uploadUrl = uploadUrl.Replace("https://", "http://", StringComparison.OrdinalIgnoreCase);
        var publicUrl = await GetPublicUrlAsync(objectKey);

        return new PresignedUrlResult
        {
            UploadUrl = uploadUrl,
            PublicUrl = publicUrl,
            ObjectKey = objectKey,
        };
    }

    public Task<string> GetPublicUrlAsync(string objectKey)
    {
        if (!string.IsNullOrEmpty(_publicBaseUrl))
        {
            return Task.FromResult($"{_publicBaseUrl.TrimEnd('/')}/{objectKey}");
        }

        // Fall back to a long-lived presigned GET URL (readable but not a stable public URL).
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = objectKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddDays(7),
        };
        return Task.FromResult(_client.GetPreSignedURL(request));
    }
}
