using System.ComponentModel.DataAnnotations;

namespace eShopModernized.Models;

/// <summary>
/// Catalog type/category lookup entity.
/// Unchanged from legacy — simple lookup table.
/// </summary>
public class CatalogType
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Type { get; set; } = string.Empty;
}
