using System.ComponentModel.DataAnnotations;

namespace eShopModernized.Models;

/// <summary>
/// Catalog brand lookup entity.
/// Unchanged from legacy — simple lookup table.
/// </summary>
public class CatalogBrand
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Brand { get; set; } = string.Empty;
}
