using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Novacart.Api.Models.Dtos.Products;
using Novacart.Api.Models.Entities;
using Novacart.Api.Services;

namespace Novacart.Api.Controllers.Admin;

/// <summary>P2-8 product and inventory management for admin/sysadmin users.</summary>
[ApiController]
[Route("api/admin/products")]
[Authorize(Roles = RoleNames.AdminRoles)]
public class AdminProductsController : ControllerBase
{
    private readonly IAdminProductService _products;
    private readonly ISquareCatalogueService _square;

    public AdminProductsController(IAdminProductService products, ISquareCatalogueService square)
    {
        _products = products;
        _square = square;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AdminProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? q,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        return Ok(await _products.GetAllAsync(q, isActive, page, pageSize));
    }

    [HttpGet("categories")]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryOptionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories() => Ok(await _products.GetCategoriesAsync());

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id) => Ok(await _products.GetByIdAsync(id));

    [HttpPost]
    [ProducesResponseType(typeof(AdminProductDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] AdminProductUpsertRequest request)
    {
        var created = await _products.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AdminProductDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] AdminProductUpsertRequest request)
        => Ok(await _products.UpdateAsync(id, request));

    /// <summary>Soft-delete a product by deactivating it.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _products.DeactivateAsync(id);
        return NoContent();
    }

    /// <summary>Sync products from Square Catalogue API (sandbox).</summary>
    [HttpPost("sync-square")]
    [ProducesResponseType(typeof(SquareSyncResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SquareSyncResultDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SyncSquare()
    {
        var result = await _square.SyncProductsAsync();
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}
