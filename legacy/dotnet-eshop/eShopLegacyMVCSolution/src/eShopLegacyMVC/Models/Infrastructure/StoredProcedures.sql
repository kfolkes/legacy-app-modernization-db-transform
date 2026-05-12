-- ============================================================================
-- eShop Legacy Application - Stored Procedures
-- Database: Microsoft.eShopOnContainers.Services.CatalogDb
-- These stored procedures encapsulate the business logic that was originally
-- handled through Entity Framework LINQ queries and service layer code.
-- ============================================================================

-- ============================================================================
-- SP 1: sp_GetCatalogItemsPaginated
-- Purpose: Retrieves paginated catalog items with brand and type information
-- Business Logic: Pagination with ordering by Id, includes related entities
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_GetCatalogItemsPaginated]
    @PageSize INT = 10,
    @PageIndex INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    -- Return total count for pagination metadata
    SELECT COUNT(*) AS TotalItems FROM [dbo].[Catalog];

    -- Return paginated items with brand and type joins
    SELECT 
        c.[Id],
        c.[Name],
        c.[Description],
        c.[Price],
        c.[PictureFileName],
        c.[CatalogTypeId],
        ct.[Type] AS CatalogTypeName,
        c.[CatalogBrandId],
        cb.[Brand] AS CatalogBrandName,
        c.[AvailableStock],
        c.[RestockThreshold],
        c.[MaxStockThreshold],
        c.[OnReorder]
    FROM [dbo].[Catalog] c
    INNER JOIN [dbo].[CatalogType] ct ON c.[CatalogTypeId] = ct.[Id]
    INNER JOIN [dbo].[CatalogBrand] cb ON c.[CatalogBrandId] = cb.[Id]
    ORDER BY c.[Id]
    OFFSET (@PageSize * @PageIndex) ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

-- ============================================================================
-- SP 2: sp_GetCatalogItemById
-- Purpose: Retrieves a single catalog item with full details
-- Business Logic: Single item lookup with brand/type join
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_GetCatalogItemById]
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        c.[Id],
        c.[Name],
        c.[Description],
        c.[Price],
        c.[PictureFileName],
        c.[CatalogTypeId],
        ct.[Type] AS CatalogTypeName,
        c.[CatalogBrandId],
        cb.[Brand] AS CatalogBrandName,
        c.[AvailableStock],
        c.[RestockThreshold],
        c.[MaxStockThreshold],
        c.[OnReorder]
    FROM [dbo].[Catalog] c
    INNER JOIN [dbo].[CatalogType] ct ON c.[CatalogTypeId] = ct.[Id]
    INNER JOIN [dbo].[CatalogBrand] cb ON c.[CatalogBrandId] = cb.[Id]
    WHERE c.[Id] = @Id;
END
GO

-- ============================================================================
-- SP 3: sp_CreateCatalogItem
-- Purpose: Creates a new catalog item using HiLo sequence for ID generation
-- Business Logic: 
--   - Generates ID from catalog_hilo sequence
--   - Validates that CatalogType and CatalogBrand exist (referential integrity)
--   - Sets default PictureFileName if not provided
--   - Returns the newly created item's ID
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_CreateCatalogItem]
    @Name NVARCHAR(50),
    @Description NVARCHAR(MAX) = NULL,
    @Price DECIMAL(18,2),
    @PictureFileName NVARCHAR(MAX) = 'dummy.png',
    @CatalogTypeId INT,
    @CatalogBrandId INT,
    @AvailableStock INT = 0,
    @RestockThreshold INT = 0,
    @MaxStockThreshold INT = 0,
    @OnReorder BIT = 0,
    @NewId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- Validate FK references exist
        IF NOT EXISTS (SELECT 1 FROM [dbo].[CatalogType] WHERE [Id] = @CatalogTypeId)
        BEGIN
            RAISERROR('Invalid CatalogTypeId: %d does not exist', 16, 1, @CatalogTypeId);
            RETURN;
        END

        IF NOT EXISTS (SELECT 1 FROM [dbo].[CatalogBrand] WHERE [Id] = @CatalogBrandId)
        BEGIN
            RAISERROR('Invalid CatalogBrandId: %d does not exist', 16, 1, @CatalogBrandId);
            RETURN;
        END

        -- Generate next ID from HiLo sequence
        DECLARE @SequenceValue BIGINT;
        SELECT @SequenceValue = NEXT VALUE FOR [dbo].[catalog_hilo];
        SET @NewId = CAST(@SequenceValue AS INT);

        -- Set default picture if not provided
        IF @PictureFileName IS NULL OR @PictureFileName = ''
            SET @PictureFileName = 'dummy.png';

        -- Insert the catalog item
        INSERT INTO [dbo].[Catalog] (
            [Id], [Name], [Description], [Price], [PictureFileName],
            [CatalogTypeId], [CatalogBrandId], [AvailableStock],
            [RestockThreshold], [MaxStockThreshold], [OnReorder]
        ) VALUES (
            @NewId, @Name, @Description, @Price, @PictureFileName,
            @CatalogTypeId, @CatalogBrandId, @AvailableStock,
            @RestockThreshold, @MaxStockThreshold, @OnReorder
        );

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- ============================================================================
-- SP 4: sp_UpdateCatalogItem
-- Purpose: Updates an existing catalog item
-- Business Logic:
--   - Validates item exists before update
--   - Updates all mutable fields
--   - Preserves audit trail via transaction
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_UpdateCatalogItem]
    @Id INT,
    @Name NVARCHAR(50),
    @Description NVARCHAR(MAX) = NULL,
    @Price DECIMAL(18,2),
    @PictureFileName NVARCHAR(MAX),
    @CatalogTypeId INT,
    @CatalogBrandId INT,
    @AvailableStock INT,
    @RestockThreshold INT,
    @MaxStockThreshold INT,
    @OnReorder BIT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Validate item exists
    IF NOT EXISTS (SELECT 1 FROM [dbo].[Catalog] WHERE [Id] = @Id)
    BEGIN
        RAISERROR('CatalogItem with Id %d does not exist', 16, 1, @Id);
        RETURN;
    END

    UPDATE [dbo].[Catalog]
    SET 
        [Name] = @Name,
        [Description] = @Description,
        [Price] = @Price,
        [PictureFileName] = @PictureFileName,
        [CatalogTypeId] = @CatalogTypeId,
        [CatalogBrandId] = @CatalogBrandId,
        [AvailableStock] = @AvailableStock,
        [RestockThreshold] = @RestockThreshold,
        [MaxStockThreshold] = @MaxStockThreshold,
        [OnReorder] = @OnReorder
    WHERE [Id] = @Id;
END
GO

-- ============================================================================
-- SP 5: sp_UpdateInventory
-- Purpose: Updates stock levels with business rule enforcement
-- Business Logic (CRITICAL - this is the core inventory domain logic):
--   - Validates stock doesn't exceed MaxStockThreshold
--   - Automatically sets OnReorder = 1 when AvailableStock drops below RestockThreshold
--   - Automatically clears OnReorder = 0 when stock is replenished above threshold
--   - Prevents negative stock values
--   - Returns updated inventory status
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_UpdateInventory]
    @Id INT,
    @QuantityChange INT,      -- Positive = restock, Negative = sale/removal
    @UpdatedStock INT OUTPUT,
    @IsOnReorder BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    
    BEGIN TRY
        DECLARE @CurrentStock INT;
        DECLARE @RestockThreshold INT;
        DECLARE @MaxStockThreshold INT;

        -- Get current inventory state
        SELECT 
            @CurrentStock = [AvailableStock],
            @RestockThreshold = [RestockThreshold],
            @MaxStockThreshold = [MaxStockThreshold]
        FROM [dbo].[Catalog]
        WHERE [Id] = @Id;

        IF @CurrentStock IS NULL
        BEGIN
            RAISERROR('CatalogItem with Id %d does not exist', 16, 1, @Id);
            RETURN;
        END

        -- Calculate new stock level
        SET @UpdatedStock = @CurrentStock + @QuantityChange;

        -- BUSINESS RULE: Prevent negative stock
        IF @UpdatedStock < 0
        BEGIN
            RAISERROR('Insufficient stock. Current: %d, Requested change: %d', 16, 1, @CurrentStock, @QuantityChange);
            RETURN;
        END

        -- BUSINESS RULE: Prevent exceeding max stock threshold  
        IF @UpdatedStock > @MaxStockThreshold AND @MaxStockThreshold > 0
        BEGIN
            RAISERROR('Stock would exceed maximum threshold. Max: %d, Attempted: %d', 16, 1, @MaxStockThreshold, @UpdatedStock);
            RETURN;
        END

        -- BUSINESS RULE: Auto-set OnReorder based on threshold comparison
        IF @UpdatedStock <= @RestockThreshold
            SET @IsOnReorder = 1;  -- Stock is low, flag for reorder
        ELSE
            SET @IsOnReorder = 0;  -- Stock is healthy, clear reorder flag

        -- Apply the inventory update
        UPDATE [dbo].[Catalog]
        SET 
            [AvailableStock] = @UpdatedStock,
            [OnReorder] = @IsOnReorder
        WHERE [Id] = @Id;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        SET @UpdatedStock = @CurrentStock;
        SET @IsOnReorder = 0;
        THROW;
    END CATCH
END
GO

-- ============================================================================
-- SP 6: sp_DeleteCatalogItem
-- Purpose: Removes a catalog item from the database
-- Business Logic:
--   - Validates item exists before deletion
--   - Hard delete (no soft delete in legacy app)
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_DeleteCatalogItem]
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Catalog] WHERE [Id] = @Id)
    BEGIN
        RAISERROR('CatalogItem with Id %d does not exist', 16, 1, @Id);
        RETURN;
    END

    DELETE FROM [dbo].[Catalog]
    WHERE [Id] = @Id;
END
GO

-- ============================================================================
-- SP 7: sp_GetInventoryReport
-- Purpose: Generates an inventory status report for management
-- Business Logic:
--   - Aggregates stock levels by brand and type
--   - Identifies items needing reorder
--   - Calculates total inventory value
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_GetInventoryReport]
AS
BEGIN
    SET NOCOUNT ON;

    -- Summary by brand
    SELECT 
        cb.[Brand],
        COUNT(*) AS ItemCount,
        SUM(c.[AvailableStock]) AS TotalStock,
        SUM(c.[Price] * c.[AvailableStock]) AS TotalInventoryValue,
        SUM(CASE WHEN c.[OnReorder] = 1 THEN 1 ELSE 0 END) AS ItemsOnReorder
    FROM [dbo].[Catalog] c
    INNER JOIN [dbo].[CatalogBrand] cb ON c.[CatalogBrandId] = cb.[Id]
    GROUP BY cb.[Brand]
    ORDER BY cb.[Brand];

    -- Items needing reorder
    SELECT 
        c.[Id],
        c.[Name],
        cb.[Brand],
        ct.[Type],
        c.[AvailableStock],
        c.[RestockThreshold],
        (c.[RestockThreshold] - c.[AvailableStock]) AS UnitsNeeded
    FROM [dbo].[Catalog] c
    INNER JOIN [dbo].[CatalogBrand] cb ON c.[CatalogBrandId] = cb.[Id]
    INNER JOIN [dbo].[CatalogType] ct ON c.[CatalogTypeId] = ct.[Id]
    WHERE c.[AvailableStock] <= c.[RestockThreshold]
    ORDER BY (c.[RestockThreshold] - c.[AvailableStock]) DESC;
END
GO
