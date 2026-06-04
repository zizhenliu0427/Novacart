using Microsoft.AspNetCore.Mvc;

namespace Novacart.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    /// <summary>
    /// Get all products (placeholder — will connect to Square API later)
    /// </summary>
    [HttpGet]
    public IActionResult GetAll()
    {
        var products = new[]
        {
            new { Id = 1, Name = "Handmade Ceramic Mug", Price = 24.99m, Category = "Ceramics", Description = "A beautiful handmade ceramic mug." },
            new { Id = 2, Name = "Wooden Cutting Board", Price = 39.99m, Category = "Woodwork", Description = "Premium walnut cutting board." },
            new { Id = 3, Name = "Linen Tote Bag", Price = 19.99m, Category = "Textiles", Description = "Eco-friendly linen tote bag." },
            new { Id = 4, Name = "Scented Soy Candle", Price = 14.99m, Category = "Home", Description = "Lavender scented soy candle." },
            new { Id = 5, Name = "Leather Wallet", Price = 49.99m, Category = "Accessories", Description = "Hand-stitched leather wallet." }
        };

        return Ok(products);
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var product = new { Id = id, Name = "Sample Product", Price = 29.99m, Category = "General", Description = "A sample product." };
        return Ok(product);
    }
}
