using eShopModernized.Models;

namespace eShopModernized.Repositories;

/// <summary>
/// Async catalog repository interface — replaces legacy ICatalogService.
/// 
/// MIGRATION CHANGES:
///   - All methods are now async (Task<T>)
///   - CancellationToken support throughout
///   - IDisposable removed (EF Core DbContext managed by DI container)
///   - Replaces both CatalogService (EF LINQ) and CatalogServiceSP (stored procedures)
///   - Stored procedure business logic extracted to IInventoryService
/// 
/// SP MIGRATION MAP:
///   sp_GetCatalogItemsPaginated → GetItemsAsync()
///   sp_GetCatalogItemById      → GetByIdAsync()
///   sp_CreateCatalogItem       → AddAsync()
///   sp_UpdateCatalogItem       → UpdateAsync()
///   sp_DeleteCatalogItem       → DeleteAsync()
///   sp_UpdateInventory         → MOVED TO IInventoryService.UpdateStockAsync()
///   sp_GetInventoryReport      → MOVED TO IInventoryService.GetInventoryReportAsync()
/// </summary>
public interface ICatalogRepository
{
    Task<PaginatedItemsViewModel<CatalogItem>> GetItemsAsync(
        int pageSize, int pageIndex, CancellationToken cancellationToken = default);
    
    Task<CatalogItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<CatalogType>> GetTypesAsync(CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<CatalogBrand>> GetBrandsAsync(CancellationToken cancellationToken = default);
    
    Task<CatalogItem> AddAsync(CatalogItem item, CancellationToken cancellationToken = default);
    
    Task UpdateAsync(CatalogItem item, CancellationToken cancellationToken = default);
    
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
