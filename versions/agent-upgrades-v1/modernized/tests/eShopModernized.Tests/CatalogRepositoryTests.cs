using eShopModernized.Data;
using eShopModernized.Models;
using eShopModernized.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace eShopModernized.Tests;

/// <summary>
/// Repository tests using EF Core InMemory provider.
/// Validates that all stored procedure behaviors are preserved in the EF Core implementation.
/// 
/// SP → REPOSITORY TEST MAP:
///   sp_GetCatalogItemsPaginated → GetItemsAsync_ReturnsPaginatedResults
///   sp_GetCatalogItemById      → GetByIdAsync_ReturnsItemWithIncludes
///   sp_CreateCatalogItem       → AddAsync_PersistsAndReturnsItem
///   sp_UpdateCatalogItem       → UpdateAsync_ModifiesExistingItem
///   sp_DeleteCatalogItem       → DeleteAsync_RemovesItem
/// </summary>
public class CatalogRepositoryTests : IDisposable
{
    private readonly CatalogDbContext _context;
    private readonly CatalogRepository _repository;

    public CatalogRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CatalogDbContext(options);
        var logger = Mock.Of<ILogger<CatalogRepository>>();
        _repository = new CatalogRepository(_context, logger);

        SeedTestData();
    }

    // ========================================================================
    // sp_GetCatalogItemsPaginated → GetItemsAsync
    // ========================================================================

    [Fact]
    public async Task GetItemsAsync_ReturnsPaginatedResults()
    {
        var result = await _repository.GetItemsAsync(pageSize: 3, pageIndex: 0);

        result.Data.Should().HaveCount(3);
        result.TotalItems.Should().Be(5); // 5 seeded items
        result.PageSize.Should().Be(3);
        result.PageIndex.Should().Be(0);
        result.TotalPages.Should().Be(2);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetItemsAsync_SecondPage_ReturnsRemainingItems()
    {
        var result = await _repository.GetItemsAsync(pageSize: 3, pageIndex: 1);

        result.Data.Should().HaveCount(2); // 5 total, 3 on first page = 2 remaining
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task GetItemsAsync_IncludesBrandAndType()
    {
        // SP equivalent: INNER JOIN CatalogType, INNER JOIN CatalogBrand
        var result = await _repository.GetItemsAsync(pageSize: 10, pageIndex: 0);

        var firstItem = result.Data.First();
        firstItem.CatalogBrand.Should().NotBeNull();
        firstItem.CatalogBrand.Brand.Should().NotBeEmpty();
        firstItem.CatalogType.Should().NotBeNull();
        firstItem.CatalogType.Type.Should().NotBeEmpty();
    }

    // ========================================================================
    // sp_GetCatalogItemById → GetByIdAsync
    // ========================================================================

    [Fact]
    public async Task GetByIdAsync_ExistingItem_ReturnsWithIncludes()
    {
        var item = await _repository.GetByIdAsync(1);

        item.Should().NotBeNull();
        item!.Name.Should().Be("Test Mug");
        item.CatalogBrand.Should().NotBeNull();
        item.CatalogType.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentItem_ReturnsNull()
    {
        // SP equivalent: Returns empty result set
        var item = await _repository.GetByIdAsync(999);
        item.Should().BeNull();
    }

    // ========================================================================
    // sp_CreateCatalogItem → AddAsync
    // ========================================================================

    [Fact]
    public async Task AddAsync_PersistsNewItem()
    {
        var newItem = new CatalogItem
        {
            Name = "New Product",
            Description = "New Description",
            Price = 25.00m,
            PictureFileName = "new.png",
            CatalogTypeId = 1,
            CatalogBrandId = 1,
            AvailableStock = 50,
            RestockThreshold = 10,
            MaxStockThreshold = 100
        };

        var result = await _repository.AddAsync(newItem);

        result.Should().NotBeNull();
        result.Name.Should().Be("New Product");
        
        // Verify it's persisted
        var fromDb = await _context.CatalogItems.FindAsync(result.Id);
        fromDb.Should().NotBeNull();
    }

    // ========================================================================
    // sp_UpdateCatalogItem → UpdateAsync
    // ========================================================================

    [Fact]
    public async Task UpdateAsync_ModifiesExistingItem()
    {
        var item = await _repository.GetByIdAsync(1);
        item!.Name = "Updated Mug Name";
        item.Price = 99.99m;

        await _repository.UpdateAsync(item);

        var updated = await _context.CatalogItems.FindAsync(1);
        updated!.Name.Should().Be("Updated Mug Name");
        updated.Price.Should().Be(99.99m);
    }

    // ========================================================================
    // sp_DeleteCatalogItem → DeleteAsync
    // ========================================================================

    [Fact]
    public async Task DeleteAsync_RemovesExistingItem()
    {
        await _repository.DeleteAsync(1);

        var deleted = await _context.CatalogItems.FindAsync(1);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentItem_ThrowsInvalidOperation()
    {
        // SP equivalent: RAISERROR('CatalogItem with Id %d does not exist')
        var act = () => _repository.DeleteAsync(999);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*does not exist*");
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    private void SeedTestData()
    {
        var brand1 = new CatalogBrand { Id = 1, Brand = ".NET" };
        var brand2 = new CatalogBrand { Id = 2, Brand = "Azure" };
        var type1 = new CatalogType { Id = 1, Type = "Mug" };
        var type2 = new CatalogType { Id = 2, Type = "T-Shirt" };

        _context.CatalogBrands.AddRange(brand1, brand2);
        _context.CatalogTypes.AddRange(type1, type2);

        _context.CatalogItems.AddRange(
            new CatalogItem { Id = 1, Name = "Test Mug", Price = 10m, PictureFileName = "1.png", CatalogTypeId = 1, CatalogBrandId = 1, AvailableStock = 100, RestockThreshold = 10, MaxStockThreshold = 200 },
            new CatalogItem { Id = 2, Name = "Test Shirt", Price = 15m, PictureFileName = "2.png", CatalogTypeId = 2, CatalogBrandId = 1, AvailableStock = 50, RestockThreshold = 10, MaxStockThreshold = 150 },
            new CatalogItem { Id = 3, Name = "Azure Mug", Price = 12m, PictureFileName = "3.png", CatalogTypeId = 1, CatalogBrandId = 2, AvailableStock = 5, RestockThreshold = 10, MaxStockThreshold = 100, OnReorder = true },
            new CatalogItem { Id = 4, Name = "Azure Shirt", Price = 20m, PictureFileName = "4.png", CatalogTypeId = 2, CatalogBrandId = 2, AvailableStock = 75, RestockThreshold = 15, MaxStockThreshold = 200 },
            new CatalogItem { Id = 5, Name = ".NET Sheet", Price = 8m, PictureFileName = "5.png", CatalogTypeId = 1, CatalogBrandId = 1, AvailableStock = 30, RestockThreshold = 5, MaxStockThreshold = 100 }
        );

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
