using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Novacart.Api.Models.Entities;
using Novacart.Api.Services;
using Novacart.Api.Storage;

namespace Novacart.Api.Controllers.Admin;

/// <summary>
/// Generates presigned S3 upload URLs for admin product images. The browser uploads
/// directly to S3 (LocalStack in dev), then stores the returned public URL as the
/// product's ImageUrl — the backend never proxies the file body.
/// </summary>
[ApiController]
[Route("api/admin/uploads")]
[Authorize(Roles = RoleNames.AdminRoles)]
public class UploadsController : ControllerBase
{
    private readonly IS3StorageService _storage;

    public UploadsController(IS3StorageService storage) => _storage = storage;

    public class PresignRequest
    {
        /// <summary>Original filename, used to derive the object key + extension.</summary>
        public string FileName { get; set; } = string.Empty;
        /// <summary>MIME type, e.g. "image/jpeg".</summary>
        public string ContentType { get; set; } = "image/jpeg";
    }

    /// <summary>Get a short-lived presigned PUT URL + the resulting public object URL.</summary>
    [HttpPost("presign")]
    [ProducesResponseType(typeof(PresignedUrlResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Presign([FromBody] PresignRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FileName))
            throw AppException.Unprocessable("FileName is required.");

        // Stable, collision-free key: products/{guid}.{ext}
        var ext = Path.GetExtension(request.FileName);
        if (string.IsNullOrEmpty(ext)) ext = ".jpg";
        var objectKey = $"products/{Guid.NewGuid():N}{ext.ToLowerInvariant()}";

        var result = await _storage.GeneratePresignedUploadUrlAsync(objectKey, request.ContentType);
        return Ok(result);
    }
}
