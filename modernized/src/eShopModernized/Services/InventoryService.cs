using eShopModernized.Data;
using eShopModernized.Models;
using Microsoft.EntityFrameworkCore;

namespace eShopModernized.Services;

/// <summary>
/// Inventory service implementation — ALL business logic extracted from stored procedures.
/// 
/// SP TO C# MIGRATION MAP:
/// ========================
/// 
/// sp_UpdateInventory:
///   - BUSINESS RULE 1 (negative stock prevention)     → CatalogItem.AdjustStock() domain method
///   - BUSINESS RULE 2 (max threshold enforcement)     → CatalogItem.AdjustStock() domain method
///   - BUSINESS RULE 3 (auto OnReorder flag)            → CatalogItem.AdjustStock() domain method
///   - BUSINESS RULE 4 (atomic transaction)             → EF Core SaveChangesAsync() (implicit transaction)
///   - OUTPUT parameters (@UpdatedStock, @IsOnReorder)  → InventoryUpdateResult return object
/// 
/// sp_GetInventoryReport:
///   - Brand aggregation (GROUP BY)                     → EF Core LINQ GroupBy with projection
///   - Reorder identification (WHERE stock <= threshold) → EF Core LINQ Where with projection
///   - Total inventory value (SUM)                      → EF Core LINQ Sum
/// 
/// ADVANTAGES OF C# OVER SP:
///   1. Unit testable without database
///   2. IDE support (refactoring, find references, debugging)
///   3. Shared validation with API layer (same models)
///   4. Async execution (non-blocking I/O)
///   5. Type safety (compile-time checking)
///   6. Version controlled alongside application code
/// </summary>
public class InventoryService : IInventoryService
{
    private readonly CatalogDbContext _db;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(CatalogDbContext db, ILogger<InventoryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Replaces sp_UpdateInventory stored procedure entirely.
    /// 
    /// The stored procedure had these sections:
    ///   1. BEGIN TRANSACTION
    ///   2. SELECT current stock values
    ///   3. Calculate new stock
    ///   4. Validate business rules (negative stock, max threshold)
    ///   5. Calculate OnReorder flag
    ///   6. UPDATE Catalog SET AvailableStock, OnReorder
    ///   7. COMMIT TRANSACTION
    ///   8. Return via OUTPUT parameters
    ///   
    /// All of this is now in CatalogItem.AdjustStock() (domain method) 
    /// + this service method (persistence + logging).
    /// </summary>
    public async Task<InventoryUpdateResult> UpdateStockAsync(
        int catalogItemId, int quantityChange, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Updating inventory. CatalogItemId={CatalogItemId}, QuantityChange={QuantityChange}",
            catalogItemId, quantityChange);

        var item = await _db.CatalogItems.FindAsync(new object[] { catalogItemId }, cancellationToken);
        if (item == null)
        {
            throw new InvalidOperationException($"CatalogItem with Id {catalogItemId} does not exist");
        }

        // Business logic is in the domain model (extracted from SP)
        var result = item.AdjustStock(quantityChange);

        // EF Core tracks the changes — SaveChangesAsync creates an implicit transaction
        // This replaces the explicit BEGIN TRAN / COMMIT / ROLLBACK in the SP
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Inventory updated. CatalogItemId={CatalogItemId}, NewStock={NewStock}, OnReorder={OnReorder}",
            catalogItemId, result.UpdatedStock, result.IsOnReorder);

        return result;
    }

    /// <summary>
    /// Replaces sp_GetInventoryReport stored procedure.
    /// 
    /// The SP returned two result sets:
    ///   Result Set 1: GROUP BY Brand with SUM(Stock), SUM(Price*Stock), COUNT(OnReorder)
    ///   Result Set 2: Items WHERE AvailableStock <= RestockThreshold ORDER BY UnitsNeeded DESC
    ///   
    /// EF Core LINQ produces the same results with type-safe projections.
    /// </summary>
    public async Task<InventoryReport> GetInventoryReportAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating inventory report");

        // Result Set 1: Brand summaries (replaces SP GROUP BY query)
        var brandSummaries = await _db.CatalogItems
            .Include(c => c.CatalogBrand)
            .GroupBy(c => c.CatalogBrand.Brand)
            .Select(g => new BrandInventorySummary
            {
                Brand = g.Key,
                ItemCount = g.Count(),
                TotalStock = g.Sum(c => c.AvailableStock),
                TotalInventoryValue = g.Sum(c => c.Price * c.AvailableStock),
                ItemsOnReorder = g.Count(c => c.OnReorder)
            })
            .OrderBy(b => b.Brand)
            .ToListAsync(cancellationToken);

        // Result Set 2: Items needing reorder (replaces SP WHERE/ORDER BY query)
        var reorderItems = await _db.CatalogItems
            .Include(c => c.CatalogBrand)
            .Include(c => c.CatalogType)
            .Where(c => c.AvailableStock <= c.RestockThreshold)
            .OrderByDescending(c => c.RestockThreshold - c.AvailableStock)
            .Select(c => new ReorderItem
            {
                Id = c.Id,
                Name = c.Name,
                Brand = c.CatalogBrand.Brand,
                Type = c.CatalogType.Type,
                AvailableStock = c.AvailableStock,
                RestockThreshold = c.RestockThreshold,
                UnitsNeeded = c.RestockThreshold - c.AvailableStock
            })
            .ToListAsync(cancellationToken);

        var totalValue = await _db.CatalogItems
            .SumAsync(c => c.Price * c.AvailableStock, cancellationToken);

        var totalCount = await _db.CatalogItems.CountAsync(cancellationToken);

        return new InventoryReport
        {
            BrandSummaries = brandSummaries,
            ItemsNeedingReorder = reorderItems,
            TotalInventoryValue = totalValue,
            TotalItemCount = totalCount
        };
    }
}
