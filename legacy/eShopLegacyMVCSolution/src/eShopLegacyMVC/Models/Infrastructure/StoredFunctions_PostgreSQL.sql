-- ============================================================================
-- eShop Legacy Application — PostgreSQL Stored Functions
-- Database: eshop_catalog_db (PostgreSQL equivalent of CatalogDb)
--
-- PURPOSE: PL/pgSQL translations of the 7 T-SQL stored procedures from
-- StoredProcedures.sql. This file serves as the MIGRATION INTERMEDIATE step:
--
--   Step 1: T-SQL Stored Procedures   (SQL Server - legacy)
--   Step 2: PL/pgSQL Functions        (PostgreSQL - this file)   ← YOU ARE HERE
--   Step 3: C# EF Core LINQ           (.NET 10 - modernized)
--
-- Each function is labeled SP[N] to map back to its T-SQL counterpart.
-- Inline comments call out every T-SQL → PL/pgSQL syntax difference.
-- ============================================================================

-- ============================================================================
-- SCHEMA SETUP: PostgreSQL equivalents of SQL Server objects
-- ============================================================================

-- T-SQL: CREATE SEQUENCE [dbo].[catalog_hilo] START WITH 1 INCREMENT BY 10
-- PL/pgSQL: CREATE SEQUENCE (same concept, different schema qualifier)
-- DIFFERENCE: No [dbo]. schema prefix — PostgreSQL uses public schema by default
CREATE SEQUENCE IF NOT EXISTS catalog_hilo START WITH 1 INCREMENT BY 10;
CREATE SEQUENCE IF NOT EXISTS catalog_brand_hilo START WITH 1 INCREMENT BY 10;
CREATE SEQUENCE IF NOT EXISTS catalog_type_hilo START WITH 1 INCREMENT BY 10;

-- T-SQL: NVARCHAR(50) → PL/pgSQL: VARCHAR(50) or TEXT
-- DIFFERENCE: PostgreSQL has no NVARCHAR — all strings are Unicode by default
-- T-SQL: BIT → PL/pgSQL: BOOLEAN
-- DIFFERENCE: BIT is 0/1, BOOLEAN is TRUE/FALSE
-- T-SQL: DECIMAL(18,2) → PL/pgSQL: NUMERIC(18,2)
-- DIFFERENCE: DECIMAL and NUMERIC are synonyms in both, but PG convention is NUMERIC
-- T-SQL: IDENTITY(1,1) → PL/pgSQL: GENERATED ALWAYS AS IDENTITY or SERIAL
-- DIFFERENCE: SERIAL is a shorthand; GENERATED ALWAYS is SQL-standard

CREATE TABLE IF NOT EXISTS catalog_brand (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    -- T-SQL: NVARCHAR(100) → PG: VARCHAR(100)
    brand VARCHAR(100) NOT NULL
);

CREATE TABLE IF NOT EXISTS catalog_type (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    -- T-SQL: NVARCHAR(100) → PG: VARCHAR(100)
    type VARCHAR(100) NOT NULL
);

CREATE TABLE IF NOT EXISTS catalog (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    -- T-SQL: NVARCHAR(50) → PG: VARCHAR(50)
    name VARCHAR(50) NOT NULL,
    -- T-SQL: NVARCHAR(MAX) → PG: TEXT (no max-length variant in PG)
    description TEXT,
    -- T-SQL: DECIMAL(18,2) → PG: NUMERIC(18,2)
    price NUMERIC(18,2) NOT NULL,
    -- T-SQL: NVARCHAR(MAX) → PG: TEXT
    picture_file_name TEXT NOT NULL DEFAULT 'dummy.png',
    catalog_type_id INTEGER NOT NULL REFERENCES catalog_type(id),
    catalog_brand_id INTEGER NOT NULL REFERENCES catalog_brand(id),
    available_stock INTEGER NOT NULL DEFAULT 0,
    restock_threshold INTEGER NOT NULL DEFAULT 0,
    max_stock_threshold INTEGER NOT NULL DEFAULT 0,
    -- T-SQL: BIT → PG: BOOLEAN
    on_reorder BOOLEAN NOT NULL DEFAULT FALSE
);

-- ============================================================================
-- SP 1 → PG Function 1: get_catalog_items_paginated
-- T-SQL: sp_GetCatalogItemsPaginated
--
-- KEY DIFFERENCES:
--   DECLARE @var TYPE     → DECLARE var TYPE (no @ prefix)
--   SET NOCOUNT ON        → Not needed in PG (no row count messages by default)
--   OFFSET/FETCH NEXT     → LIMIT/OFFSET (reversed parameter order)
--   INNER JOIN with [dbo]. → JOIN without schema prefix
--   Returns two result sets in T-SQL → PG functions return one result;
--     use OUT parameter for total count + RETURNS TABLE for items
-- ============================================================================
CREATE OR REPLACE FUNCTION get_catalog_items_paginated(
    p_page_size INTEGER DEFAULT 10,
    p_page_index INTEGER DEFAULT 0,
    -- T-SQL uses separate SELECT for count; PG uses OUT parameter
    OUT total_items BIGINT
)
RETURNS TABLE (
    id INTEGER,
    name VARCHAR(50),
    description TEXT,
    price NUMERIC(18,2),
    picture_file_name TEXT,
    catalog_type_id INTEGER,
    catalog_type_name VARCHAR(100),
    catalog_brand_id INTEGER,
    catalog_brand_name VARCHAR(100),
    available_stock INTEGER,
    restock_threshold INTEGER,
    max_stock_threshold INTEGER,
    on_reorder BOOLEAN
)
LANGUAGE plpgsql
AS $$
BEGIN
    -- T-SQL: SET NOCOUNT ON → Not needed in PG
    -- T-SQL: SELECT COUNT(*) AS TotalItems FROM [dbo].[Catalog]
    -- PG: Use OUT parameter instead of separate result set
    SELECT COUNT(*) INTO total_items FROM catalog;

    -- T-SQL: ORDER BY c.[Id] OFFSET (@PageSize * @PageIndex) ROWS
    --        FETCH NEXT @PageSize ROWS ONLY
    -- PG:   ORDER BY c.id LIMIT p_page_size OFFSET (p_page_size * p_page_index)
    -- DIFFERENCE: PG uses LIMIT/OFFSET instead of OFFSET/FETCH NEXT
    -- DIFFERENCE: No square bracket quoting — PG uses double quotes if needed
    RETURN QUERY
    SELECT
        c.id,
        c.name,
        c.description,
        c.price,
        c.picture_file_name,
        c.catalog_type_id,
        ct.type AS catalog_type_name,
        c.catalog_brand_id,
        cb.brand AS catalog_brand_name,
        c.available_stock,
        c.restock_threshold,
        c.max_stock_threshold,
        c.on_reorder
    FROM catalog c
    INNER JOIN catalog_type ct ON c.catalog_type_id = ct.id
    INNER JOIN catalog_brand cb ON c.catalog_brand_id = cb.id
    ORDER BY c.id
    -- T-SQL: OFFSET (@PageSize * @PageIndex) ROWS FETCH NEXT @PageSize ROWS ONLY
    -- PG: LIMIT + OFFSET (simpler syntax)
    LIMIT p_page_size OFFSET (p_page_size * p_page_index);
END;
$$;


-- ============================================================================
-- SP 2 → PG Function 2: get_catalog_item_by_id
-- T-SQL: sp_GetCatalogItemById
--
-- KEY DIFFERENCES:
--   Minimal — straightforward SELECT translation
--   @Id → p_id (PG convention: no @, use p_ prefix for parameters)
-- ============================================================================
CREATE OR REPLACE FUNCTION get_catalog_item_by_id(p_id INTEGER)
RETURNS TABLE (
    id INTEGER,
    name VARCHAR(50),
    description TEXT,
    price NUMERIC(18,2),
    picture_file_name TEXT,
    catalog_type_id INTEGER,
    catalog_type_name VARCHAR(100),
    catalog_brand_id INTEGER,
    catalog_brand_name VARCHAR(100),
    available_stock INTEGER,
    restock_threshold INTEGER,
    max_stock_threshold INTEGER,
    on_reorder BOOLEAN
)
LANGUAGE plpgsql
AS $$
BEGIN
    -- T-SQL: SET NOCOUNT ON → Not needed
    RETURN QUERY
    SELECT
        c.id,
        c.name,
        c.description,
        c.price,
        c.picture_file_name,
        c.catalog_type_id,
        ct.type AS catalog_type_name,
        c.catalog_brand_id,
        cb.brand AS catalog_brand_name,
        c.available_stock,
        c.restock_threshold,
        c.max_stock_threshold,
        c.on_reorder
    FROM catalog c
    INNER JOIN catalog_type ct ON c.catalog_type_id = ct.id
    INNER JOIN catalog_brand cb ON c.catalog_brand_id = cb.id
    WHERE c.id = p_id;
END;
$$;


-- ============================================================================
-- SP 3 → PG Function 3: create_catalog_item
-- T-SQL: sp_CreateCatalogItem
--
-- KEY DIFFERENCES:
--   BEGIN TRY...END TRY BEGIN CATCH...END CATCH → BEGIN...EXCEPTION WHEN OTHERS THEN
--   RAISERROR('msg', 16, 1, @var) → RAISE EXCEPTION 'msg: %', var
--   DECLARE @SequenceValue BIGINT → DECLARE v_sequence_value BIGINT
--   SELECT @var = NEXT VALUE FOR [dbo].[seq] → SELECT nextval('seq') INTO var
--   @NewId INT OUTPUT → Use RETURNS INTEGER or OUT parameter
--   BEGIN TRANSACTION / COMMIT / ROLLBACK → Same concept, but PG auto-wraps
--     single statements in transactions; explicit BEGIN only for multi-statement
-- ============================================================================
CREATE OR REPLACE FUNCTION create_catalog_item(
    p_name VARCHAR(50),
    p_description TEXT DEFAULT NULL,
    p_price NUMERIC(18,2) DEFAULT 0,
    p_picture_file_name TEXT DEFAULT 'dummy.png',
    p_catalog_type_id INTEGER DEFAULT 0,
    p_catalog_brand_id INTEGER DEFAULT 0,
    p_available_stock INTEGER DEFAULT 0,
    p_restock_threshold INTEGER DEFAULT 0,
    p_max_stock_threshold INTEGER DEFAULT 0,
    p_on_reorder BOOLEAN DEFAULT FALSE
)
RETURNS INTEGER  -- T-SQL used OUTPUT parameter @NewId; PG returns directly
LANGUAGE plpgsql
AS $$
DECLARE
    -- T-SQL: DECLARE @SequenceValue BIGINT; DECLARE @NewId INT;
    -- PG: DECLARE v_sequence_value BIGINT; v_new_id INTEGER;
    -- DIFFERENCE: No @ prefix for variables; use v_ convention
    v_sequence_value BIGINT;
    v_new_id INTEGER;
BEGIN
    -- T-SQL: BEGIN TRANSACTION → PG: function body is already in a transaction
    -- DIFFERENCE: PG functions run inside the caller's transaction by default
    -- For explicit control, the caller uses BEGIN/COMMIT

    -- T-SQL: IF NOT EXISTS (SELECT 1 FROM [dbo].[CatalogType] WHERE [Id] = @CatalogTypeId)
    -- PG: Same pattern, different quoting
    IF NOT EXISTS (SELECT 1 FROM catalog_type WHERE id = p_catalog_type_id) THEN
        -- T-SQL: RAISERROR('Invalid CatalogTypeId: %d does not exist', 16, 1, @CatalogTypeId)
        -- PG: RAISE EXCEPTION 'message: %', variable
        -- DIFFERENCE: RAISERROR uses %d/%s with severity/state; RAISE EXCEPTION uses %
        RAISE EXCEPTION 'Invalid catalog_type_id: % does not exist', p_catalog_type_id;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM catalog_brand WHERE id = p_catalog_brand_id) THEN
        RAISE EXCEPTION 'Invalid catalog_brand_id: % does not exist', p_catalog_brand_id;
    END IF;

    -- T-SQL: SELECT @SequenceValue = NEXT VALUE FOR [dbo].[catalog_hilo]
    -- PG: SELECT nextval('catalog_hilo') INTO v_sequence_value
    -- DIFFERENCE: NEXT VALUE FOR → nextval('sequence_name')
    SELECT nextval('catalog_hilo') INTO v_sequence_value;
    v_new_id := v_sequence_value::INTEGER;

    -- T-SQL: IF @PictureFileName IS NULL OR @PictureFileName = ''
    -- PG: Same logic, uses COALESCE or NULLIF for idiomatic PG
    IF COALESCE(p_picture_file_name, '') = '' THEN
        p_picture_file_name := 'dummy.png';
    END IF;

    INSERT INTO catalog (
        name, description, price, picture_file_name,
        catalog_type_id, catalog_brand_id, available_stock,
        restock_threshold, max_stock_threshold, on_reorder
    ) OVERRIDING SYSTEM VALUE  -- Needed because id is GENERATED ALWAYS
    VALUES (
        p_name, p_description, p_price, p_picture_file_name,
        p_catalog_type_id, p_catalog_brand_id, p_available_stock,
        p_restock_threshold, p_max_stock_threshold, p_on_reorder
    );

    RETURN v_new_id;

-- T-SQL: END TRY BEGIN CATCH ROLLBACK TRANSACTION; THROW; END CATCH
-- PG: EXCEPTION WHEN OTHERS THEN RAISE
-- DIFFERENCE: TRY/CATCH → BEGIN...EXCEPTION block
EXCEPTION
    WHEN OTHERS THEN
        RAISE;  -- Re-raise the exception (equivalent to THROW in T-SQL)
END;
$$;


-- ============================================================================
-- SP 4 → PG Function 4: update_catalog_item
-- T-SQL: sp_UpdateCatalogItem
--
-- KEY DIFFERENCES:
--   Minimal — straightforward UPDATE translation
--   RAISERROR → RAISE EXCEPTION
-- ============================================================================
CREATE OR REPLACE FUNCTION update_catalog_item(
    p_id INTEGER,
    p_name VARCHAR(50),
    p_description TEXT DEFAULT NULL,
    p_price NUMERIC(18,2) DEFAULT 0,
    p_picture_file_name TEXT DEFAULT 'dummy.png',
    p_catalog_type_id INTEGER DEFAULT 0,
    p_catalog_brand_id INTEGER DEFAULT 0,
    p_available_stock INTEGER DEFAULT 0,
    p_restock_threshold INTEGER DEFAULT 0,
    p_max_stock_threshold INTEGER DEFAULT 0,
    p_on_reorder BOOLEAN DEFAULT FALSE
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    -- T-SQL: IF NOT EXISTS (SELECT 1 FROM [dbo].[Catalog] WHERE [Id] = @Id)
    IF NOT EXISTS (SELECT 1 FROM catalog WHERE id = p_id) THEN
        -- T-SQL: RAISERROR('CatalogItem with Id %d does not exist', 16, 1, @Id)
        RAISE EXCEPTION 'CatalogItem with Id % does not exist', p_id;
    END IF;

    UPDATE catalog
    SET
        name = p_name,
        description = p_description,
        price = p_price,
        picture_file_name = p_picture_file_name,
        catalog_type_id = p_catalog_type_id,
        catalog_brand_id = p_catalog_brand_id,
        available_stock = p_available_stock,
        restock_threshold = p_restock_threshold,
        max_stock_threshold = p_max_stock_threshold,
        on_reorder = p_on_reorder
    WHERE id = p_id;
END;
$$;


-- ============================================================================
-- SP 5 → PG Function 5: update_inventory  ⚠️ CRITICAL BUSINESS LOGIC
-- T-SQL: sp_UpdateInventory
--
-- KEY DIFFERENCES (this is the most complex translation):
--   DECLARE @CurrentStock INT → DECLARE v_current_stock INTEGER
--   BEGIN TRY...END TRY BEGIN CATCH...END CATCH → BEGIN...EXCEPTION
--   RAISERROR(...) → RAISE EXCEPTION '...'
--   @@TRANCOUNT → Not needed (PG doesn't have nested transaction counting)
--   BEGIN TRANSACTION / COMMIT / ROLLBACK → PG function is already transactional
--   OUTPUT parameters → OUT parameters in function signature
--   SET @var = expr → var := expr (PG uses := assignment)
-- ============================================================================
CREATE OR REPLACE FUNCTION update_inventory(
    p_id INTEGER,
    p_quantity_change INTEGER,    -- Positive = restock, Negative = sale/removal
    OUT p_updated_stock INTEGER,  -- T-SQL: @UpdatedStock INT OUTPUT → PG: OUT parameter
    OUT p_is_on_reorder BOOLEAN   -- T-SQL: @IsOnReorder BIT OUTPUT → PG: OUT BOOLEAN
)
LANGUAGE plpgsql
AS $$
DECLARE
    -- T-SQL: DECLARE @CurrentStock INT; DECLARE @RestockThreshold INT; ...
    -- PG: DECLARE v_current_stock INTEGER; v_restock_threshold INTEGER; ...
    -- DIFFERENCE: No @ prefix, use v_ for local variables
    v_current_stock INTEGER;
    v_restock_threshold INTEGER;
    v_max_stock_threshold INTEGER;
BEGIN
    -- T-SQL: BEGIN TRANSACTION → PG: implicit (function runs in caller's txn)
    -- DIFFERENCE: PG doesn't need explicit BEGIN TRANSACTION inside functions
    -- @@TRANCOUNT is irrelevant — PG uses savepoints for nested error handling

    -- Get current inventory state
    SELECT
        available_stock,
        restock_threshold,
        max_stock_threshold
    INTO
        v_current_stock,
        v_restock_threshold,
        v_max_stock_threshold
    FROM catalog
    WHERE id = p_id;

    -- T-SQL: IF @CurrentStock IS NULL → PG: IF NOT FOUND
    -- DIFFERENCE: PG provides FOUND variable after SELECT INTO
    IF NOT FOUND THEN
        RAISE EXCEPTION 'CatalogItem with Id % does not exist', p_id;
    END IF;

    -- Calculate new stock level
    -- T-SQL: SET @UpdatedStock = @CurrentStock + @QuantityChange
    -- PG: p_updated_stock := v_current_stock + p_quantity_change
    -- DIFFERENCE: := assignment instead of SET =
    p_updated_stock := v_current_stock + p_quantity_change;

    -- BUSINESS RULE 1: Prevent negative stock
    IF p_updated_stock < 0 THEN
        -- T-SQL: RAISERROR('Insufficient stock. Current: %d, Requested change: %d', 16, 1, ...)
        -- PG: RAISE EXCEPTION '... Current: %, Requested change: %', var1, var2
        -- DIFFERENCE: %d → %, severity/state not used in PG
        RAISE EXCEPTION 'Insufficient stock. Current: %, Requested change: %',
            v_current_stock, p_quantity_change;
    END IF;

    -- BUSINESS RULE 2: Prevent exceeding max stock threshold
    IF p_updated_stock > v_max_stock_threshold AND v_max_stock_threshold > 0 THEN
        RAISE EXCEPTION 'Stock would exceed maximum threshold. Max: %, Attempted: %',
            v_max_stock_threshold, p_updated_stock;
    END IF;

    -- BUSINESS RULE 3: Auto-set OnReorder based on threshold comparison
    -- T-SQL: IF @UpdatedStock <= @RestockThreshold SET @IsOnReorder = 1 ELSE SET @IsOnReorder = 0
    -- PG: Use BOOLEAN TRUE/FALSE instead of BIT 1/0
    -- DIFFERENCE: BIT 0/1 → BOOLEAN FALSE/TRUE
    IF p_updated_stock <= v_restock_threshold THEN
        p_is_on_reorder := TRUE;   -- Stock is low, flag for reorder
    ELSE
        p_is_on_reorder := FALSE;  -- Stock is healthy, clear reorder flag
    END IF;

    -- BUSINESS RULE 4: Apply the inventory update atomically
    UPDATE catalog
    SET
        available_stock = p_updated_stock,
        on_reorder = p_is_on_reorder
    WHERE id = p_id;

    -- T-SQL: COMMIT TRANSACTION → PG: implicit (caller controls transaction)

-- T-SQL: END TRY BEGIN CATCH ROLLBACK TRANSACTION; ... THROW; END CATCH
-- PG: EXCEPTION block handles errors
-- DIFFERENCE: TRY/CATCH → EXCEPTION; ROLLBACK → automatic on exception
EXCEPTION
    WHEN OTHERS THEN
        p_updated_stock := v_current_stock;
        p_is_on_reorder := FALSE;
        RAISE;  -- Re-raise (equivalent to THROW in T-SQL)
END;
$$;


-- ============================================================================
-- SP 6 → PG Function 6: delete_catalog_item
-- T-SQL: sp_DeleteCatalogItem
--
-- KEY DIFFERENCES:
--   Minimal — RAISERROR → RAISE EXCEPTION
-- ============================================================================
CREATE OR REPLACE FUNCTION delete_catalog_item(p_id INTEGER)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM catalog WHERE id = p_id) THEN
        -- T-SQL: RAISERROR('CatalogItem with Id %d does not exist', 16, 1, @Id)
        RAISE EXCEPTION 'CatalogItem with Id % does not exist', p_id;
    END IF;

    DELETE FROM catalog WHERE id = p_id;
END;
$$;


-- ============================================================================
-- SP 7 → PG Function 7: get_inventory_report
-- T-SQL: sp_GetInventoryReport
--
-- KEY DIFFERENCES:
--   T-SQL returns two result sets (brand summary + reorder items)
--   PG functions return ONE result set — must split into two functions
--     or use RETURNS TABLE with a discriminator column
--   Here we split into two functions for clarity (PG best practice)
--
--   CASE WHEN c.[OnReorder] = 1 → CASE WHEN c.on_reorder = TRUE
--   (or simply: CASE WHEN c.on_reorder THEN 1 ELSE 0 END)
--   SUM(CASE WHEN ...) → COUNT(*) FILTER (WHERE ...) — PG-specific, more idiomatic
-- ============================================================================

-- Part A: Brand summary report
CREATE OR REPLACE FUNCTION get_inventory_report_by_brand()
RETURNS TABLE (
    brand VARCHAR(100),
    item_count BIGINT,
    total_stock BIGINT,
    total_inventory_value NUMERIC,
    items_on_reorder BIGINT
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT
        cb.brand,
        COUNT(*) AS item_count,
        SUM(c.available_stock)::BIGINT AS total_stock,
        SUM(c.price * c.available_stock) AS total_inventory_value,
        -- T-SQL: SUM(CASE WHEN c.[OnReorder] = 1 THEN 1 ELSE 0 END)
        -- PG: COUNT(*) FILTER (WHERE condition) — more idiomatic
        -- DIFFERENCE: FILTER clause is PostgreSQL-specific, cleaner than CASE
        COUNT(*) FILTER (WHERE c.on_reorder = TRUE) AS items_on_reorder
    FROM catalog c
    INNER JOIN catalog_brand cb ON c.catalog_brand_id = cb.id
    GROUP BY cb.brand
    ORDER BY cb.brand;
END;
$$;

-- Part B: Items needing reorder
CREATE OR REPLACE FUNCTION get_inventory_reorder_items()
RETURNS TABLE (
    id INTEGER,
    name VARCHAR(50),
    brand VARCHAR(100),
    type VARCHAR(100),
    available_stock INTEGER,
    restock_threshold INTEGER,
    units_needed INTEGER
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT
        c.id,
        c.name,
        cb.brand,
        ct.type,
        c.available_stock,
        c.restock_threshold,
        (c.restock_threshold - c.available_stock) AS units_needed
    FROM catalog c
    INNER JOIN catalog_brand cb ON c.catalog_brand_id = cb.id
    INNER JOIN catalog_type ct ON c.catalog_type_id = ct.id
    WHERE c.available_stock <= c.restock_threshold
    ORDER BY (c.restock_threshold - c.available_stock) DESC;
END;
$$;


-- ============================================================================
-- QUICK REFERENCE: T-SQL → PL/pgSQL Syntax Differences Used Above
-- ============================================================================
--
-- | # | T-SQL (SQL Server)                        | PL/pgSQL (PostgreSQL)                      |
-- |---|-------------------------------------------|--------------------------------------------|
-- | 1 | DECLARE @variable TYPE                    | DECLARE variable TYPE (no @)               |
-- | 2 | BEGIN TRY ... END TRY BEGIN CATCH ... END | BEGIN ... EXCEPTION WHEN OTHERS THEN ... END|
-- | 3 | #TempTable / CREATE TABLE #temp           | CREATE TEMP TABLE temp (session-scoped)     |
-- | 4 | @@TRANCOUNT, nested transactions          | Savepoints (SAVEPOINT/ROLLBACK TO)          |
-- | 5 | CHARINDEX/PATINDEX/STUFF                  | POSITION/REGEXP_MATCHES/OVERLAY             |
-- | 6 | GETDATE()/DATEADD/DATEDIFF               | NOW()/interval arithmetic (+ INTERVAL '1d') |
-- | 7 | IDENTITY(1,1)                             | GENERATED ALWAYS AS IDENTITY or SERIAL      |
-- | 8 | NVARCHAR(MAX)                             | TEXT (all strings Unicode by default)        |
-- | 9 | BIT (0/1)                                 | BOOLEAN (TRUE/FALSE)                        |
-- |10 | RAISERROR('msg', severity, state)         | RAISE EXCEPTION 'msg'                       |
-- |11 | SET NOCOUNT ON                            | Not needed (no row-count messages)           |
-- |12 | OFFSET x ROWS FETCH NEXT y ROWS ONLY     | LIMIT y OFFSET x                            |
-- |13 | NEXT VALUE FOR [dbo].[sequence]           | nextval('sequence')                          |
-- |14 | [dbo].[TableName]                         | table_name (lowercase, no schema prefix)     |
-- |15 | DECIMAL(18,2)                             | NUMERIC(18,2)                                |
-- ============================================================================
