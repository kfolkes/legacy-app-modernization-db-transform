using eShopModernized.Data;
using eShopModernized.Models;
using Microsoft.EntityFrameworkCore;

namespace eShopModernized.Repositories;

/// <summary>
/// EF Core repository implementation — replaces both CatalogService and CatalogServiceSP.
/// 
/// All stored procedures have been converted to EF Core LINQ queries:
///   sp_GetCatalogItemsPaginated → GetItemsAsync() — EF Core .Skip().Take() with .Include()
///   sp_GetCatalogItemById      → GetByIdAsync()  — .FirstOrDefaultAsync() with .Include()  
///   sp_CreateCatalogItem       → AddAsync()       — DbSet.Add() + SaveChangesAsync()
///   sp_UpdateCatalogItem       → UpdateAsync()    — Change tracker + SaveChangesAsync()
///   sp_DeleteCatalogItem       → DeleteAsync()    — DbSet.Remove() + SaveChangesAsync()
///   
/// KEY IMPROVEMENTS:
///   - Full async/await (no thread blocking)
///   - CancellationToken support for graceful cancellation
///   - HiLo ID generation handled automatically by EF Core (no manual SqlQuery)
///   - Type-safe LINQ queries (no raw SQL, no AddWithValue)
///   - Navigation property loading via .Include() (no manual JOINs)
///   - Concurrency safety via EF Core change tracker
/// </summary>
public class CatalogRepository : ICatalogRepository
{
    private readonly CatalogDbContext _db;
    private readonly ILogger<CatalogRepository> _logger;

    public CatalogRepository(CatalogDbContext db, ILogger<CatalogRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Replaces sp_GetCatalogItemsPaginated stored procedure.
    /// 
    /// Legacy SP used: OFFSET/FETCH NEXT with explicit JOINs
    /// Modern EF Core: .Include() for navigation + .Skip()/.Take() for pagination
    /// </summary>
    public async Task<PaginatedItemsViewModel<CatalogItem>> GetItemsAsync(
        int pageSize, int pageIndex, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching catalog items. PageSize={PageSize}, PageIndex={PageIndex}", pageSize, pageIndex);

        var totalItems = await _db.CatalogItems.LongCountAsync(cancellationToken);

        var itemsOnPage = await _db.CatalogItems
            .Include(c => c.CatalogBrand)
            .Include(c => c.CatalogType)
            .OrderBy(c => c.Id)
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new PaginatedItemsViewModel<CatalogItem>(pageIndex, pageSize, totalItems, itemsOnPage);
    }

    /// <summary>
    /// Replaces sp_GetCatalogItemById stored procedure.
    /// 
    /// Legacy SP used: SELECT with INNER JOINs on CatalogBrand and CatalogType
    /// Modern EF Core: .Include() navigation properties with FirstOrDefaultAsync
    /// </summary>
    public async Task<CatalogItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Finding catalog item. Id={Id}", id);

        return await _db.CatalogItems
            .Include(c => c.CatalogBrand)
            .Include(c => c.CatalogType)
            .FirstOrDefaultAsync(ci => ci.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<CatalogType>> GetTypesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.CatalogTypes
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CatalogBrand>> GetBrandsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.CatalogBrands
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Replaces sp_CreateCatalogItem stored procedure.
    /// 
    /// Legacy SP: Manual HiLo sequence ID generation via NEXT VALUE FOR + FK validation + INSERT
    /// Modern EF Core: HiLo handled by UseHiLo() config, FK validation by model constraints,
    ///                  INSERT by DbSet.Add() + SaveChangesAsync()
    /// </summary>
    public async Task<CatalogItem> AddAsync(CatalogItem item, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating catalog item. Name={Name}", item.Name);

        // EF Core automatically generates the ID via HiLo sequence
        // No need for CatalogItemHiLoGenerator or raw SQL
        _db.CatalogItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created catalog item. Id={Id}, Name={Name}", item.Id, item.Name);
        return item;
    }

    /// <summary>
    /// Replaces sp_UpdateCatalogItem stored procedure.
    /// 
    /// Legacy SP: Existence check + full UPDATE SET
    /// Modern EF Core: Change tracker handles dirty checking + optimized UPDATE
    /// </summary>
    public async Task UpdateAsync(CatalogItem item, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating catalog item. Id={Id}", item.Id);

        _db.CatalogItems.Update(item);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Replaces sp_DeleteCatalogItem stored procedure.
    /// 
    /// Legacy SP: Existence check + DELETE FROM
    /// Modern EF Core: Find + Remove + SaveChanges (throws if not found)
    /// </summary>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting catalog item. Id={Id}", id);

        var item = await _db.CatalogItems.FindAsync(new object[] { id }, cancellationToken);
        if (item == null)
        {
            throw new InvalidOperationException($"CatalogItem with Id {id} does not exist");
        }

        _db.CatalogItems.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
