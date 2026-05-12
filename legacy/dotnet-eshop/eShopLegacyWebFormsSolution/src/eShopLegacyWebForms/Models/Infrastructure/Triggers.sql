-- =============================================================================
-- SQL Server Triggers — eShop Legacy Database
-- These triggers represent hidden business logic embedded in the database layer.
-- In the modernized .NET 10 application, each trigger is replaced by:
--   - EF Core SaveChangesInterceptor (for audit logging)
--   - Domain Events + MediatR handlers (for notifications/alerts)
--   - IChangeTracker patterns (for change detection)
-- =============================================================================

-- =============================================================================
-- TRIGGER 1: trg_CatalogItem_AuditLog
-- Purpose: Tracks all INSERT, UPDATE, DELETE operations on the Catalog table.
-- Modernized as: EF Core SaveChangesInterceptor → AuditLogInterceptor.cs
-- =============================================================================
CREATE TRIGGER [dbo].[trg_CatalogItem_AuditLog]
ON [dbo].[Catalog]
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Action NVARCHAR(10);
    DECLARE @UserId NVARCHAR(128);

    -- Determine the action type
    IF EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted)
        SET @Action = 'UPDATE';
    ELSE IF EXISTS (SELECT 1 FROM inserted)
        SET @Action = 'INSERT';
    ELSE
        SET @Action = 'DELETE';

    -- Get the current user context (stored in session context by the application)
    SET @UserId = CONVERT(NVARCHAR(128), SESSION_CONTEXT(N'UserId'));

    -- Log inserts and updates (new values)
    IF @Action IN ('INSERT', 'UPDATE')
    BEGIN
        INSERT INTO [dbo].[AuditLog] (
            TableName, RecordId, Action, FieldName, OldValue, NewValue,
            ChangedBy, ChangedAt
        )
        SELECT
            'Catalog',
            i.Id,
            @Action,
            'Name',
            CASE WHEN @Action = 'UPDATE' THEN d.Name ELSE NULL END,
            i.Name,
            ISNULL(@UserId, SYSTEM_USER),
            GETUTCDATE()
        FROM inserted i
        LEFT JOIN deleted d ON i.Id = d.Id
        WHERE @Action = 'INSERT' OR i.Name <> d.Name;

        -- Log price changes separately (important for business audit)
        INSERT INTO [dbo].[AuditLog] (
            TableName, RecordId, Action, FieldName, OldValue, NewValue,
            ChangedBy, ChangedAt
        )
        SELECT
            'Catalog',
            i.Id,
            @Action,
            'Price',
            CASE WHEN @Action = 'UPDATE' THEN CAST(d.Price AS NVARCHAR(50)) ELSE NULL END,
            CAST(i.Price AS NVARCHAR(50)),
            ISNULL(@UserId, SYSTEM_USER),
            GETUTCDATE()
        FROM inserted i
        LEFT JOIN deleted d ON i.Id = d.Id
        WHERE @Action = 'INSERT' OR i.Price <> d.Price;
    END

    -- Log deletes (old values)
    IF @Action = 'DELETE'
    BEGIN
        INSERT INTO [dbo].[AuditLog] (
            TableName, RecordId, Action, FieldName, OldValue, NewValue,
            ChangedBy, ChangedAt
        )
        SELECT
            'Catalog',
            d.Id,
            'DELETE',
            'Name',
            d.Name,
            NULL,
            ISNULL(@UserId, SYSTEM_USER),
            GETUTCDATE()
        FROM deleted d;
    END
END
GO

-- =============================================================================
-- TRIGGER 2: trg_Inventory_StockAlert
-- Purpose: Fires when stock drops below the reorder threshold.
--          Inserts a row into StockAlerts table for downstream processing.
-- Business Rule: If AvailableStock < ReorderThreshold AND OnReorder = 0,
--                create an alert and auto-set OnReorder = 1.
-- Modernized as: Domain Event (StockBelowThresholdEvent) + MediatR handler
-- =============================================================================
CREATE TRIGGER [dbo].[trg_Inventory_StockAlert]
ON [dbo].[Catalog]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Only fire when AvailableStock column changes
    IF UPDATE(AvailableStock)
    BEGIN
        -- Find items that just dropped below threshold
        INSERT INTO [dbo].[StockAlerts] (
            CatalogItemId, CatalogItemName, AvailableStock,
            ReorderThreshold, AlertType, CreatedAt, IsProcessed
        )
        SELECT
            i.Id,
            i.Name,
            i.AvailableStock,
            i.RestockThreshold,
            CASE
                WHEN i.AvailableStock = 0 THEN 'OUT_OF_STOCK'
                WHEN i.AvailableStock < i.RestockThreshold THEN 'LOW_STOCK'
            END,
            GETUTCDATE(),
            0  -- Not yet processed
        FROM inserted i
        INNER JOIN deleted d ON i.Id = d.Id
        WHERE i.AvailableStock < i.RestockThreshold
          AND d.AvailableStock >= d.RestockThreshold  -- Was above threshold before
          AND i.OnReorder = 0;

        -- Auto-set OnReorder flag for items that dropped below threshold
        UPDATE c
        SET c.OnReorder = 1
        FROM [dbo].[Catalog] c
        INNER JOIN inserted i ON c.Id = i.Id
        INNER JOIN deleted d ON c.Id = d.Id
        WHERE i.AvailableStock < i.RestockThreshold
          AND d.AvailableStock >= d.RestockThreshold
          AND c.OnReorder = 0;
    END
END
GO

-- =============================================================================
-- TRIGGER 3: trg_CatalogItem_PriceHistory
-- Purpose: Maintains a price history table whenever price changes.
--          Used by reporting for price trend analysis.
-- Modernized as: Domain Event (PriceChangedEvent) + Event Hub stream
-- =============================================================================
CREATE TRIGGER [dbo].[trg_CatalogItem_PriceHistory]
ON [dbo].[Catalog]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF UPDATE(Price)
    BEGIN
        INSERT INTO [dbo].[PriceHistory] (
            CatalogItemId, OldPrice, NewPrice,
            PriceChangePercent, ChangedBy, ChangedAt
        )
        SELECT
            i.Id,
            d.Price,
            i.Price,
            CASE
                WHEN d.Price = 0 THEN 100.00
                ELSE ROUND(((i.Price - d.Price) / d.Price) * 100, 2)
            END,
            CONVERT(NVARCHAR(128), SESSION_CONTEXT(N'UserId')),
            GETUTCDATE()
        FROM inserted i
        INNER JOIN deleted d ON i.Id = d.Id
        WHERE i.Price <> d.Price;
    END
END
GO

-- =============================================================================
-- Supporting Tables for Triggers
-- =============================================================================

-- Audit log table (used by trg_CatalogItem_AuditLog)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLog')
CREATE TABLE [dbo].[AuditLog] (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    TableName       NVARCHAR(128)   NOT NULL,
    RecordId        INT             NOT NULL,
    Action          NVARCHAR(10)    NOT NULL,  -- INSERT, UPDATE, DELETE
    FieldName       NVARCHAR(128)   NOT NULL,
    OldValue        NVARCHAR(MAX)   NULL,
    NewValue        NVARCHAR(MAX)   NULL,
    ChangedBy       NVARCHAR(128)   NOT NULL,
    ChangedAt       DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);
GO

-- Stock alerts table (used by trg_Inventory_StockAlert)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StockAlerts')
CREATE TABLE [dbo].[StockAlerts] (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    CatalogItemId   INT             NOT NULL,
    CatalogItemName NVARCHAR(256)   NOT NULL,
    AvailableStock  INT             NOT NULL,
    ReorderThreshold INT            NOT NULL,
    AlertType       NVARCHAR(20)    NOT NULL,  -- OUT_OF_STOCK, LOW_STOCK
    CreatedAt       DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    IsProcessed     BIT             NOT NULL DEFAULT 0,
    ProcessedAt     DATETIME2       NULL
);
GO

-- Price history table (used by trg_CatalogItem_PriceHistory)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PriceHistory')
CREATE TABLE [dbo].[PriceHistory] (
    Id                  INT IDENTITY(1,1) PRIMARY KEY,
    CatalogItemId       INT             NOT NULL,
    OldPrice            DECIMAL(18,2)   NOT NULL,
    NewPrice            DECIMAL(18,2)   NOT NULL,
    PriceChangePercent  DECIMAL(8,2)    NOT NULL,
    ChangedBy           NVARCHAR(128)   NULL,
    ChangedAt           DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);
GO
