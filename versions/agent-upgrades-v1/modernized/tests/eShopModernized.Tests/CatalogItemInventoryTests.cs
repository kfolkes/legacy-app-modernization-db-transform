using eShopModernized.Models;
using FluentAssertions;

namespace eShopModernized.Tests;

/// <summary>
/// Unit tests for CatalogItem.AdjustStock() domain method.
/// 
/// These tests validate ALL business rules that were previously embedded
/// in the sp_UpdateInventory stored procedure. Every test case maps to a
/// specific business rule from the legacy SP, ensuring zero regression.
/// 
/// SP BUSINESS RULE → TEST MAPPING:
///   RULE 1 (Negative stock prevention)     → NegativeStockPrevention tests
///   RULE 2 (Max threshold enforcement)     → MaxStockThreshold tests
///   RULE 3 (Auto OnReorder flag)            → OnReorder tests
///   RULE 4 (Atomic operations)              → Tested in integration tests (EF Core transaction)
/// </summary>
public class CatalogItemInventoryTests
{
    // ========================================================================
    // RULE 1: Stock cannot go negative
    // SP equivalent: IF @UpdatedStock < 0 → RAISERROR
    // ========================================================================

    [Fact]
    public void AdjustStock_SaleWithSufficientStock_ReducesStock()
    {
        // Arrange — item with 100 units
        var item = CreateTestItem(availableStock: 100, restockThreshold: 10, maxStockThreshold: 200);

        // Act — sell 30 units
        var result = item.AdjustStock(-30);

        // Assert
        result.UpdatedStock.Should().Be(70);
        item.AvailableStock.Should().Be(70);
    }

    [Fact]
    public void AdjustStock_SaleExactlyToZero_Succeeds()
    {
        var item = CreateTestItem(availableStock: 50, restockThreshold: 10, maxStockThreshold: 200);

        var result = item.AdjustStock(-50);

        result.UpdatedStock.Should().Be(0);
        item.AvailableStock.Should().Be(0);
    }

    [Fact]
    public void AdjustStock_SaleExceedsStock_ThrowsInvalidOperation()
    {
        // Arrange — item with only 10 units
        var item = CreateTestItem(availableStock: 10, restockThreshold: 5, maxStockThreshold: 100);

        // Act & Assert — trying to sell 15 should fail (SP would RAISERROR)
        var act = () => item.AdjustStock(-15);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Insufficient stock*Current: 10*Requested change: -15*");
    }

    [Fact]
    public void AdjustStock_SaleFromZeroStock_ThrowsInvalidOperation()
    {
        var item = CreateTestItem(availableStock: 0, restockThreshold: 5, maxStockThreshold: 100);

        var act = () => item.AdjustStock(-1);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Insufficient stock*");
    }

    // ========================================================================
    // RULE 2: Stock cannot exceed maximum threshold
    // SP equivalent: IF @UpdatedStock > @MaxStockThreshold AND @MaxStockThreshold > 0
    // ========================================================================

    [Fact]
    public void AdjustStock_RestockWithinThreshold_Succeeds()
    {
        var item = CreateTestItem(availableStock: 50, restockThreshold: 10, maxStockThreshold: 200);

        var result = item.AdjustStock(100);

        result.UpdatedStock.Should().Be(150);
        item.AvailableStock.Should().Be(150);
    }

    [Fact]
    public void AdjustStock_RestockToExactMax_Succeeds()
    {
        var item = CreateTestItem(availableStock: 150, restockThreshold: 10, maxStockThreshold: 200);

        var result = item.AdjustStock(50);

        result.UpdatedStock.Should().Be(200);
    }

    [Fact]
    public void AdjustStock_RestockExceedsMax_ThrowsInvalidOperation()
    {
        // Arrange — item near max threshold
        var item = CreateTestItem(availableStock: 190, restockThreshold: 10, maxStockThreshold: 200);

        // Act & Assert — trying to add 20 would exceed max (SP would RAISERROR)
        var act = () => item.AdjustStock(20);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Stock would exceed maximum threshold*Max: 200*Attempted: 210*");
    }

    [Fact]
    public void AdjustStock_MaxThresholdZero_AllowsUnlimitedRestock()
    {
        // SP equivalent: IF @MaxStockThreshold > 0 (only enforces when > 0)
        var item = CreateTestItem(availableStock: 1000, restockThreshold: 10, maxStockThreshold: 0);

        var result = item.AdjustStock(5000);

        result.UpdatedStock.Should().Be(6000);
    }

    // ========================================================================
    // RULE 3: Auto OnReorder flag management
    // SP equivalent: IF @UpdatedStock <= @RestockThreshold → SET @IsOnReorder = 1
    //                ELSE → SET @IsOnReorder = 0
    // ========================================================================

    [Fact]
    public void AdjustStock_StockDropsBelowThreshold_SetsOnReorderTrue()
    {
        var item = CreateTestItem(availableStock: 15, restockThreshold: 10, maxStockThreshold: 200);
        item.OnReorder.Should().BeFalse(); // starts healthy

        var result = item.AdjustStock(-10); // drops to 5, below threshold of 10

        result.IsOnReorder.Should().BeTrue();
        item.OnReorder.Should().BeTrue();
    }

    [Fact]
    public void AdjustStock_StockAtExactThreshold_SetsOnReorderTrue()
    {
        // SP: IF @UpdatedStock <= @RestockThreshold (includes equal-to)
        var item = CreateTestItem(availableStock: 20, restockThreshold: 10, maxStockThreshold: 200);

        item.AdjustStock(-10); // drops to exactly 10 = threshold

        item.OnReorder.Should().BeTrue();
    }

    [Fact]
    public void AdjustStock_RestockAboveThreshold_ClearsOnReorderFlag()
    {
        var item = CreateTestItem(availableStock: 5, restockThreshold: 10, maxStockThreshold: 200);
        item.OnReorder = true; // already flagged for reorder

        var result = item.AdjustStock(20); // restocks to 25, above threshold of 10

        result.IsOnReorder.Should().BeFalse();
        item.OnReorder.Should().BeFalse();
    }

    [Fact]
    public void AdjustStock_RestockStillBelowThreshold_KeepsOnReorderTrue()
    {
        var item = CreateTestItem(availableStock: 2, restockThreshold: 10, maxStockThreshold: 200);
        item.OnReorder = true;

        var result = item.AdjustStock(3); // restocks to 5, still below threshold of 10

        result.IsOnReorder.Should().BeTrue();
    }

    // ========================================================================
    // RULE 4: Result object matches SP OUTPUT parameters
    // ========================================================================

    [Fact]
    public void AdjustStock_ReturnsCorrectResultObject()
    {
        var item = CreateTestItem(id: 42, availableStock: 100, restockThreshold: 10, maxStockThreshold: 200);

        var result = item.AdjustStock(-30);

        result.CatalogItemId.Should().Be(42);
        result.UpdatedStock.Should().Be(70);
        result.QuantityChanged.Should().Be(-30);
        result.IsOnReorder.Should().BeFalse();
    }

    [Fact]
    public void AdjustStock_ZeroChange_IsNoOp()
    {
        var item = CreateTestItem(availableStock: 50, restockThreshold: 10, maxStockThreshold: 200);

        var result = item.AdjustStock(0);

        result.UpdatedStock.Should().Be(50);
        item.AvailableStock.Should().Be(50);
    }

    // ========================================================================
    // Helper
    // ========================================================================

    private static CatalogItem CreateTestItem(
        int id = 1,
        int availableStock = 100,
        int restockThreshold = 10,
        int maxStockThreshold = 200)
    {
        return new CatalogItem
        {
            Id = id,
            Name = "Test Item",
            Description = "Test Description",
            Price = 10.00m,
            PictureFileName = "test.png",
            CatalogTypeId = 1,
            CatalogBrandId = 1,
            AvailableStock = availableStock,
            RestockThreshold = restockThreshold,
            MaxStockThreshold = maxStockThreshold,
            OnReorder = availableStock <= restockThreshold
        };
    }
}
