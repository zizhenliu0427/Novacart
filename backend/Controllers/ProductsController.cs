using Microsoft.AspNetCore.Mvc;
using Novacart.Api.Models.Dtos.Products;
using Novacart.Api.Services;

namespace Novacart.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _products;

    public ProductsController(IProductService products) => _products = products;

    /// <summary>
    /// List products with optional keyword search, category filter, sort, and pagination.
    /// </summary>
    /// <param name="q">Keyword search (name + description, case-insensitive).</param>
    /// <param name="categoryId">Filter by category ID.</param>
    /// <param name="sort">Sort order: newest (default), price_asc, price_desc, name_asc.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Items per page (max 100).</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? q,
        [FromQuery] int? categoryId,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);
        return Ok(await _products.GetAllAsync(q, categoryId, sort, page, pageSize));
    }

    /// <summary>Get a single product's full detail (includes metadata for the attribute table).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
        => Ok(await _products.GetByIdAsync(id));
}
