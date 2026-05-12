using eShopModernized.Models;

namespace eShopModernized.Services;

/// <summary>
/// Inventory service interface — extracted from sp_UpdateInventory and sp_GetInventoryReport.
/// 
/// This is the KEY MODERNIZATION ARTIFACT: business logic that was previously
/// embedded inside SQL stored procedures is now in a C# service that can be:
///   - Unit tested without a database
///   - Documented in code
///   - Version controlled with the application
///   - Reused across API endpoints
///   - Extended without SQL expertise
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// Updates stock for a catalog item with full business rule enforcement.
    /// Replaces sp_UpdateInventory stored procedure.
    /// </summary>
    Task<InventoryUpdateResult> UpdateStockAsync(
        int catalogItemId, int quantityChange, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates inventory report with aggregation by brand and reorder identification.
    /// Replaces sp_GetInventoryReport stored procedure.
    /// </summary>
    Task<InventoryReport> GetInventoryReportAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Inventory report model — replaces the multiple result sets from sp_GetInventoryReport.
/// </summary>
public class InventoryReport
{
    public IReadOnlyList<BrandInventorySummary> BrandSummaries { get; init; } = Array.Empty<BrandInventorySummary>();
    public IReadOnlyList<ReorderItem> ItemsNeedingReorder { get; init; } = Array.Empty<ReorderItem>();
    public decimal TotalInventoryValue { get; init; }
    public int TotalItemCount { get; init; }
}

public class BrandInventorySummary
{
    public string Brand { get; init; } = string.Empty;
    public int ItemCount { get; init; }
    public int TotalStock { get; init; }
    public decimal TotalInventoryValue { get; init; }
    public int ItemsOnReorder { get; init; }
}

public class ReorderItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Brand { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public int AvailableStock { get; init; }
    public int RestockThreshold { get; init; }
    public int UnitsNeeded { get; init; }
}
