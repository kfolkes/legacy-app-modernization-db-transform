using eShopModernized.Data;
using eShopModernized.Models;
using eShopModernized.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace eShopModernized.Tests;

/// <summary>
/// Integration tests for InventoryService — validates that the business logic
/// extracted from sp_UpdateInventory and sp_GetInventoryReport works correctly
/// with the EF Core persistence layer.
/// 
/// These tests use EF Core InMemory provider to simulate the database,
/// verifying atomic operations and data consistency.
/// </summary>
public class InventoryServiceTests : IDisposable
{
    private readonly CatalogDbContext _context;
    private readonly InventoryService _service;

    public InventoryServiceTests()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CatalogDbContext(options);
        var logger = Mock.Of<ILogger<InventoryService>>();
        _service = new InventoryService(_context, logger);

        SeedTestData();
    }

    // ========================================================================
    // sp_UpdateInventory → UpdateStockAsync (end-to-end with persistence)
    // ========================================================================

    [Fact]
    public async Task UpdateStockAsync_Sale_ReducesStockAndPersists()
    {
        var result = await _service.UpdateStockAsync(catalogItemId: 1, quantityChange: -20);

        result.UpdatedStock.Should().Be(80);
        
        // Verify persistence (SP would have this in the same transaction)
        var item = await _context.CatalogItems.FindAsync(1);
        item!.AvailableStock.Should().Be(80);
    }

    [Fact]
    public async Task UpdateStockAsync_Restock_IncreasesStockAndPersists()
    {
        var result = await _service.UpdateStockAsync(catalogItemId: 3, quantityChange: 50);

        result.UpdatedStock.Should().Be(55); // was 5, now 55
        result.IsOnReorder.Should().BeFalse(); // above threshold of 10
        
        var item = await _context.CatalogItems.FindAsync(3);
        item!.OnReorder.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateStockAsync_BelowThreshold_SetsOnReorder()
    {
        // Item 1: stock=100, threshold=10 → sell 95 units → stock=5 (below threshold)
        var result = await _service.UpdateStockAsync(catalogItemId: 1, quantityChange: -95);

        result.UpdatedStock.Should().Be(5);
        result.IsOnReorder.Should().BeTrue();
        
        var item = await _context.CatalogItems.FindAsync(1);
        item!.OnReorder.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateStockAsync_InsufficientStock_ThrowsAndDoesNotPersist()
    {
        var act = () => _service.UpdateStockAsync(catalogItemId: 1, quantityChange: -200);
        
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Insufficient stock*");

        // Verify no persistence occurred
        var item = await _context.CatalogItems.FindAsync(1);
        item!.AvailableStock.Should().Be(100); // unchanged
    }

    [Fact]
    public async Task UpdateStockAsync_NonExistentItem_ThrowsNotFound()
    {
        var act = () => _service.UpdateStockAsync(catalogItemId: 999, quantityChange: 10);
        
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*does not exist*");
    }

    // ========================================================================
    // sp_GetInventoryReport → GetInventoryReportAsync
    // ========================================================================

    [Fact]
    public async Task GetInventoryReportAsync_ReturnsBrandSummaries()
    {
        var report = await _service.GetInventoryReportAsync();

        report.BrandSummaries.Should().HaveCount(2); // .NET and Azure brands
        report.TotalItemCount.Should().Be(5);
    }

    [Fact]
    public async Task GetInventoryReportAsync_IdentifiesReorderItems()
    {
        var report = await _service.GetInventoryReportAsync();

        // Item 3 (Azure Mug) has stock=5, threshold=10 → needs reorder
        report.ItemsNeedingReorder.Should().Contain(r => r.Name == "Azure Mug");
        
        var reorderItem = report.ItemsNeedingReorder.First(r => r.Name == "Azure Mug");
        reorderItem.UnitsNeeded.Should().Be(5); // threshold(10) - stock(5) = 5
    }

    [Fact]
    public async Task GetInventoryReportAsync_CalculatesTotalValue()
    {
        var report = await _service.GetInventoryReportAsync();

        // Expected: (10*100) + (15*50) + (12*5) + (20*75) + (8*30) = 1000+750+60+1500+240 = 3550
        report.TotalInventoryValue.Should().Be(3550m);
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    private void SeedTestData()
    {
        _context.CatalogBrands.AddRange(
            new CatalogBrand { Id = 1, Brand = ".NET" },
            new CatalogBrand { Id = 2, Brand = "Azure" }
        );
        _context.CatalogTypes.AddRange(
            new CatalogType { Id = 1, Type = "Mug" },
            new CatalogType { Id = 2, Type = "T-Shirt" }
        );
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
