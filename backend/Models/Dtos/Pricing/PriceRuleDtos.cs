using System.ComponentModel.DataAnnotations;
using Novacart.Api.Models.Entities;

namespace Novacart.Api.Models.Dtos.Pricing;

public class PriceRuleDto
{
    public Guid Id { get; set; }

    /// <summary>Target product; null = category/global scope.</summary>
    public Guid? ProductId { get; set; }
    public string? ProductName { get; set; }

    /// <summary>Target category; null = product/global scope.</summary>
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }

    public PriceRuleType RuleType { get; set; }

    /// <summary>Percent (0–100), flat amount, or absolute price — depends on RuleType.</summary>
    public decimal Value { get; set; }

    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreatePriceRuleRequest
{
    public Guid? ProductId { get; set; }
    public int? CategoryId { get; set; }

    [Required]
    public PriceRuleType RuleType { get; set; }

    /// <summary>Percent (0–100), flat amount off, or absolute price.</summary>
    [Range(0, double.MaxValue)]
    public decimal Value { get; set; }

    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }

    public bool IsActive { get; set; } = true;
}
