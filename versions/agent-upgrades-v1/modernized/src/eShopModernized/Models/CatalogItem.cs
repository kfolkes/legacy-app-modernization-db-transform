using System.ComponentModel.DataAnnotations;

namespace eShopModernized.Models;

/// <summary>
/// Product catalog item — migrated from eShopLegacyMVC.Models.CatalogItem.
/// 
/// CHANGES FROM LEGACY:
///   - Added nullable annotations (C# nullable reference types)
///   - PictureUri removed from entity (was [NotMapped] / Ignored in Fluent API)
///   - Validation attributes preserved for backward compatibility
///   - Added domain methods for inventory business logic (extracted from sp_UpdateInventory)
/// </summary>
public class CatalogItem
{
    public const string DefaultPictureName = "dummy.png";

    public CatalogItem()
    {
        PictureFileName = DefaultPictureName;
        Name = string.Empty;
    }

    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; }

    public string? Description { get; set; }

    [Range(0, 1000000)]
    [DataType(DataType.Currency)]
    [RegularExpression(@"^\d+(\.\d{0,2})*$", ErrorMessage = "Price must be a positive number with maximum two decimals.")]
    public decimal Price { get; set; }

    [Display(Name = "Picture name")]
    public string PictureFileName { get; set; }

    [Display(Name = "Type")]
    public int CatalogTypeId { get; set; }

    [Display(Name = "Type")]
    public CatalogType CatalogType { get; set; } = null!;

    [Display(Name = "Brand")]
    public int CatalogBrandId { get; set; }

    [Display(Name = "Brand")]
    public CatalogBrand CatalogBrand { get; set; } = null!;

    [Range(0, 10000000, ErrorMessage = "Stock must be between 0 and 10 million.")]
    [Display(Name = "Stock")]
    public int AvailableStock { get; set; }

    [Range(0, 10000000, ErrorMessage = "Restock threshold must be between 0 and 10 million.")]
    [Display(Name = "Restock")]
    public int RestockThreshold { get; set; }

    [Range(0, 10000000, ErrorMessage = "Max stock must be between 0 and 10 million.")]
    [Display(Name = "Max stock")]
    public int MaxStockThreshold { get; set; }

    /// <summary>
    /// True if item is on reorder.
    /// BUSINESS RULE (extracted from sp_UpdateInventory):
    /// Automatically set when AvailableStock drops below RestockThreshold.
    /// </summary>
    public bool OnReorder { get; set; }

    // ---- Domain Methods (business logic extracted from stored procedures) ----

    /// <summary>
    /// Adjusts stock by the given quantity change.
    /// Extracted from sp_UpdateInventory stored procedure.
    /// 
    /// Business Rules preserved:
    /// 1. Stock cannot go negative
    /// 2. Stock cannot exceed MaxStockThreshold (when > 0)
    /// 3. OnReorder flag auto-managed based on RestockThreshold
    /// </summary>
    public InventoryUpdateResult AdjustStock(int quantityChange)
    {
        var newStock = AvailableStock + quantityChange;

        // RULE 1: Prevent negative stock
        if (newStock < 0)
            throw new InvalidOperationException(
                $"Insufficient stock. Current: {AvailableStock}, Requested change: {quantityChange}");

        // RULE 2: Prevent exceeding max threshold
        if (MaxStockThreshold > 0 && newStock > MaxStockThreshold)
            throw new InvalidOperationException(
                $"Stock would exceed maximum threshold. Max: {MaxStockThreshold}, Attempted: {newStock}");

        AvailableStock = newStock;

        // RULE 3: Auto-manage OnReorder flag
        OnReorder = AvailableStock <= RestockThreshold;

        return new InventoryUpdateResult
        {
            CatalogItemId = Id,
            UpdatedStock = AvailableStock,
            IsOnReorder = OnReorder,
            QuantityChanged = quantityChange
        };
    }
}

/// <summary>
/// Result of an inventory update operation.
/// Equivalent to the OUTPUT parameters from sp_UpdateInventory.
/// </summary>
public class InventoryUpdateResult
{
    public int CatalogItemId { get; set; }
    public int UpdatedStock { get; set; }
    public bool IsOnReorder { get; set; }
    public int QuantityChanged { get; set; }
}
