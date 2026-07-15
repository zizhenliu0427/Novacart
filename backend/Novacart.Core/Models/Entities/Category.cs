namespace Novacart.Api.Models.Entities;

/// <summary>
/// Product category. Supports nesting via the optional self-referencing <see cref="ParentId"/>.
/// </summary>
public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public int? ParentId { get; set; }
    public Category? Parent { get; set; }

    public int DisplayOrder { get; set; }

    // Navigation
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
