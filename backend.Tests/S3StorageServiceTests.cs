using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Novacart.Api.Storage;

namespace Novacart.Api.Tests;

/// <summary>
/// Tests for S3StorageService. Presigned URLs are computed locally by the AWS SDK
/// (no network call), so we can verify URL generation against a LocalStack config
/// without a running LocalStack container.
/// </summary>
public class S3StorageServiceTests
{
    /// <summary>Minimal dictionary-backed config (avoids extra NuGet deps).</summary>
    private sealed class DictConfig : IConfiguration
    {
        private readonly Dictionary<string, string?> _values;
        public DictConfig(Dictionary<string, string?> values) => _values = values;
        public string? this[string key] { get => _values.GetValueOrDefault(key); set => _values[key] = value; }
        public IEnumerable<IConfigurationSection> GetChildren() => Enumerable.Empty<IConfigurationSection>();
        public IChangeToken GetReloadToken() => NullToken.Instance;
        public IConfigurationSection GetSection(string key) => throw new NotImplementedException();
        public IEnumerable<KeyValuePair<string, string?>> GetChildKeys(
            IEnumerable<KeyValuePair<string, string?>> earlierKeys, string? parentPath) => _values;
    }

    private sealed class NullToken : IChangeToken
    {
        public static readonly NullToken Instance = new();
        public bool HasChanged => false;
        public bool ActiveChangeCallbacks => false;
        public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) => NullDisposable.Instance;
        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();
            public void Dispose() { }
        }
    }

    private static IConfiguration LocalStackConfig() => new DictConfig(new()
    {
        ["Aws:S3:Bucket"] = "test-bucket",
        ["Aws:S3:Region"] = "us-east-1",
        ["Aws:S3:ServiceUrl"] = "http://localstack:4566",
        ["Aws:S3:AccessKey"] = "test",
        ["Aws:S3:SecretKey"] = "test",
        ["Aws:S3:ForcePathStyle"] = "true",
        ["Aws:S3:PublicBaseUrl"] = "http://localhost:4566/test-bucket",
    });

    [Fact]
    public void Constructor_WithLocalStackConfig_BuildsServiceWithoutNetwork()
    {
        var svc = new S3StorageService(LocalStackConfig());
        svc.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_Throws_WhenBucketNotConfigured()
    {
        var cfg = new DictConfig(new() { ["Aws:S3:Region"] = "us-east-1" });
        var act = () => new S3StorageService(cfg);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task GeneratePresignedUploadUrlAsync_ReturnsPutUrl_AndPublicUrl()
    {
        var svc = new S3StorageService(LocalStackConfig());

        var result = await svc.GeneratePresignedUploadUrlAsync("products/abc.jpg", "image/jpeg");

        result.UploadUrl.Should().NotBeNullOrEmpty();
        // Presigned PUT URL targets the LocalStack endpoint.
        result.UploadUrl.Should().Contain("localstack:4566");
        // Public URL uses the configured base + object key.
        result.PublicUrl.Should().Be("http://localhost:4566/test-bucket/products/abc.jpg");
        result.ObjectKey.Should().Be("products/abc.jpg");
    }

    [Fact]
    public async Task GetPublicUrlAsync_UsesConfiguredBase_WhenSet()
    {
        var svc = new S3StorageService(LocalStackConfig());

        var url = await svc.GetPublicUrlAsync("products/xyz.png");

        url.Should().Be("http://localhost:4566/test-bucket/products/xyz.png");
    }
}
