# Phase 4B: SQL Server → PostgreSQL Database Migration

## Overview

This phase documents the database migration from **SQL Server** (legacy) to **Azure Database for PostgreSQL Flexible Server** (modernized). The modernized application supports **dual database providers** (SQL Server + PostgreSQL), switchable via configuration, enabling gradual migration with rollback capability.

**Migration Path (Three Steps):**

| Step | Database | Language | Purpose |
|---|---|---|---|
| **Step 1** | SQL Server | T-SQL Stored Procedures | Legacy — business logic in database |
| **Step 2** | PostgreSQL | PL/pgSQL Functions | Migration intermediate — validates schema translation |
| **Step 3** | .NET 10 | C# EF Core LINQ | Modernized — business logic in application code |

**Source Files:**
- Step 1: `legacy/.../StoredProcedures.sql` (7 T-SQL stored procedures)
- Step 2: `legacy/.../StoredFunctions_PostgreSQL.sql` (7 PL/pgSQL equivalents)
- Step 3: `modernized/src/eShopModernized/` (EF Core + C# domain logic)

---

## 1. Stored Procedure Migration Map (Labeled)

Each stored procedure is labeled **SP[N]** and mapped across all three steps:

| Label | T-SQL Stored Procedure | PL/pgSQL Function | C# EF Core Replacement | Complexity |
|---|---|---|---|---|
| **SP1** | `sp_GetCatalogItemsPaginated` | `get_catalog_items_paginated()` | `CatalogRepository.GetItemsAsync()` | Low |
| **SP2** | `sp_GetCatalogItemById` | `get_catalog_item_by_id()` | `CatalogRepository.GetByIdAsync()` | Low |
| **SP3** | `sp_CreateCatalogItem` | `create_catalog_item()` | `CatalogRepository.AddAsync()` | Medium |
| **SP4** | `sp_UpdateCatalogItem` | `update_catalog_item()` | `CatalogRepository.UpdateAsync()` | Low |
| **SP5** | `sp_UpdateInventory` | `update_inventory()` | `CatalogItem.AdjustStock()` + `InventoryService.UpdateStockAsync()` | **Critical** |
| **SP6** | `sp_DeleteCatalogItem` | `delete_catalog_item()` | `CatalogRepository.DeleteAsync()` | Low |
| **SP7** | `sp_GetInventoryReport` | `get_inventory_report_by_brand()` + `get_inventory_reorder_items()` | `InventoryService.GetInventoryReportAsync()` | Medium |

> **Note on SP7:** T-SQL returns two result sets from a single procedure. PostgreSQL functions return one result set per function, so SP7 is split into two PL/pgSQL functions. In the modernized C# code, both queries are encapsulated in a single async method.

---

## 2. Key T-SQL → PL/pgSQL Differences Reference

### ⚠️ Critical Syntax Differences

These are the areas that commonly cause issues during SQL Server → PostgreSQL migration:

| # | Category | T-SQL (SQL Server) | PL/pgSQL (PostgreSQL) | Example |
|---|---|---|---|---|
| **1** | **Variable Declaration** | `DECLARE @var TYPE` | `DECLARE var TYPE` | `@CurrentStock INT` → `v_current_stock INTEGER` |
| **2** | **Error Handling** | `BEGIN TRY...END TRY BEGIN CATCH...END CATCH` | `BEGIN...EXCEPTION WHEN OTHERS THEN...END` | See SP3, SP5 below |
| **3** | **Temp Tables** | `#TempTable` (auto-created, session-scoped) | `CREATE TEMP TABLE temp_name (...)` | No `#` prefix; explicit CREATE required |
| **4** | **Transaction Control** | `@@TRANCOUNT`, nested transactions, `BEGIN TRAN` | Savepoints (`SAVEPOINT`/`ROLLBACK TO`); functions run inside caller's transaction | SP5: `BEGIN TRANSACTION` → implicit |
| **5** | **String Functions** | `CHARINDEX`, `PATINDEX`, `STUFF` | `POSITION`, `REGEXP_MATCHES`, `OVERLAY` | `CHARINDEX(',', @str)` → `POSITION(',' IN str)` |
| **6** | **Date Functions** | `GETDATE()`, `DATEADD`, `DATEDIFF` | `NOW()`, interval arithmetic | `DATEADD(day, 7, GETDATE())` → `NOW() + INTERVAL '7 days'` |
| **7** | **Identity Columns** | `IDENTITY(1,1)` | `SERIAL` or `GENERATED ALWAYS AS IDENTITY` | `Id INT IDENTITY(1,1)` → `id INTEGER GENERATED ALWAYS AS IDENTITY` |

### Additional Syntax Differences Found in This Migration

| # | Category | T-SQL | PL/pgSQL | Impact |
|---|---|---|---|---|
| **8** | **String Types** | `NVARCHAR(MAX)`, `NVARCHAR(50)` | `TEXT`, `VARCHAR(50)` | All PG strings are Unicode; no `N` prefix needed |
| **9** | **Boolean Type** | `BIT` (0/1) | `BOOLEAN` (TRUE/FALSE) | `@OnReorder BIT = 0` → `p_on_reorder BOOLEAN DEFAULT FALSE` |
| **10** | **Decimal Type** | `DECIMAL(18,2)` | `NUMERIC(18,2)` | Functionally identical; PG convention uses NUMERIC |
| **11** | **Error Raising** | `RAISERROR('msg', 16, 1, @var)` | `RAISE EXCEPTION 'msg: %', var` | No severity/state params; `%d` → `%` |
| **12** | **Sequence Access** | `NEXT VALUE FOR [dbo].[seq]` | `nextval('seq')` | Different function syntax |
| **13** | **Row Count** | `SET NOCOUNT ON` | Not needed | PG doesn't send row count messages by default |
| **14** | **Pagination** | `OFFSET x ROWS FETCH NEXT y ROWS ONLY` | `LIMIT y OFFSET x` | Reversed parameter order |
| **15** | **Schema Prefix** | `[dbo].[TableName]` | `table_name` (public schema) | PG uses lowercase, no bracket quoting |
| **16** | **Aggregate Filter** | `SUM(CASE WHEN ... THEN 1 ELSE 0 END)` | `COUNT(*) FILTER (WHERE ...)` | PG-specific, cleaner syntax |
| **17** | **Assignment** | `SET @var = expr` | `var := expr` | Different assignment operator |
| **18** | **Output Params** | `@Param TYPE OUTPUT` | `OUT param TYPE` in function signature | Different declaration position |
| **19** | **Multiple Result Sets** | Single SP returns N result sets | One function = one result set | SP7 split into 2 PG functions |

---

## 3. Detailed SP Translation Examples

### SP5: `sp_UpdateInventory` → `update_inventory()` (Critical Business Logic)

This is the highest-complexity stored procedure — it contains 4 business rules that must be preserved exactly.

#### T-SQL (SQL Server) — Before

```sql
CREATE OR ALTER PROCEDURE [dbo].[sp_UpdateInventory]
    @Id INT,
    @QuantityChange INT,
    @UpdatedStock INT OUTPUT,          -- OUTPUT parameter
    @IsOnReorder BIT OUTPUT            -- BIT type
AS
BEGIN
    SET NOCOUNT ON;                    -- Suppress row count
    BEGIN TRANSACTION;                 -- Explicit transaction
    
    BEGIN TRY                          -- TRY/CATCH error handling
        DECLARE @CurrentStock INT;
        DECLARE @RestockThreshold INT;
        DECLARE @MaxStockThreshold INT;

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

        SET @UpdatedStock = @CurrentStock + @QuantityChange;

        IF @UpdatedStock < 0
            RAISERROR('Insufficient stock...', 16, 1, @CurrentStock, @QuantityChange);

        IF @UpdatedStock > @MaxStockThreshold AND @MaxStockThreshold > 0
            RAISERROR('Stock would exceed maximum threshold...', 16, 1, ...);

        IF @UpdatedStock <= @RestockThreshold
            SET @IsOnReorder = 1;      -- BIT = 1
        ELSE
            SET @IsOnReorder = 0;      -- BIT = 0

        UPDATE [dbo].[Catalog]
        SET [AvailableStock] = @UpdatedStock, [OnReorder] = @IsOnReorder
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
```

#### PL/pgSQL (PostgreSQL) — Migration Intermediate

```sql
CREATE OR REPLACE FUNCTION update_inventory(
    p_id INTEGER,
    p_quantity_change INTEGER,
    OUT p_updated_stock INTEGER,       -- OUT parameter (not OUTPUT)
    OUT p_is_on_reorder BOOLEAN        -- BOOLEAN (not BIT)
)
LANGUAGE plpgsql
AS $$
DECLARE                                -- No @ prefix
    v_current_stock INTEGER;
    v_restock_threshold INTEGER;
    v_max_stock_threshold INTEGER;
BEGIN                                  -- Implicit transaction
    SELECT available_stock, restock_threshold, max_stock_threshold
    INTO v_current_stock, v_restock_threshold, v_max_stock_threshold
    FROM catalog WHERE id = p_id;

    IF NOT FOUND THEN                  -- PG's FOUND variable
        RAISE EXCEPTION 'CatalogItem with Id % does not exist', p_id;
    END IF;

    p_updated_stock := v_current_stock + p_quantity_change;  -- := assignment

    IF p_updated_stock < 0 THEN
        RAISE EXCEPTION 'Insufficient stock. Current: %, ...', v_current_stock, p_quantity_change;
    END IF;

    IF p_updated_stock > v_max_stock_threshold AND v_max_stock_threshold > 0 THEN
        RAISE EXCEPTION 'Stock would exceed maximum threshold...', ...;
    END IF;

    IF p_updated_stock <= v_restock_threshold THEN
        p_is_on_reorder := TRUE;       -- BOOLEAN TRUE (not BIT 1)
    ELSE
        p_is_on_reorder := FALSE;      -- BOOLEAN FALSE (not BIT 0)
    END IF;

    UPDATE catalog
    SET available_stock = p_updated_stock, on_reorder = p_is_on_reorder
    WHERE id = p_id;

EXCEPTION                              -- EXCEPTION (not CATCH)
    WHEN OTHERS THEN
        p_updated_stock := v_current_stock;
        p_is_on_reorder := FALSE;
        RAISE;                         -- RAISE (not THROW)
END;
$$;
```

#### C# EF Core (.NET 10) — Modernized Final

```csharp
// CatalogItem.cs — Domain method (replaces SP business logic)
public InventoryUpdateResult AdjustStock(int quantityChange)
{
    var newStock = AvailableStock + quantityChange;

    if (newStock < 0)
        throw new InvalidOperationException(
            $"Insufficient stock. Current: {AvailableStock}, Requested: {quantityChange}");

    if (MaxStockThreshold > 0 && newStock > MaxStockThreshold)
        throw new InvalidOperationException(
            $"Stock would exceed max threshold. Max: {MaxStockThreshold}, Attempted: {newStock}");

    AvailableStock = newStock;
    OnReorder = AvailableStock <= RestockThreshold;

    return new InventoryUpdateResult(AvailableStock, OnReorder);
}
```

---

## 4. Architecture Diagrams

### 4.1 Before: SQL Server Architecture (Legacy)

```mermaid
graph TB
    subgraph "Legacy Application (.NET Framework 4.7.2)"
        APP[CatalogServiceSP.cs<br/>ADO.NET SqlCommand]
        CONN[Web.config<br/>Connection String ⚠️ Plaintext]
    end
    
    subgraph "SQL Server (LocalDB)"
        subgraph "Tables"
            T1[(Catalog<br/>NVARCHAR, BIT, DECIMAL)]
            T2[(CatalogBrand)]
            T3[(CatalogType)]
        end
        
        subgraph "Stored Procedures (T-SQL)"
            SP1[SP1: sp_GetCatalogItemsPaginated<br/>OFFSET/FETCH, @@ROWCOUNT]
            SP2[SP2: sp_GetCatalogItemById]
            SP3[SP3: sp_CreateCatalogItem<br/>BEGIN TRY/CATCH, RAISERROR]
            SP4[SP4: sp_UpdateCatalogItem]
            SP5[SP5: sp_UpdateInventory<br/>🔴 4 Business Rules<br/>@@TRANCOUNT, BIT flags]
            SP6[SP6: sp_DeleteCatalogItem]
            SP7[SP7: sp_GetInventoryReport<br/>2 Result Sets, CASE WHEN]
        end
        
        subgraph "Sequences"
            SEQ[catalog_hilo<br/>catalog_brand_hilo<br/>catalog_type_hilo<br/>NEXT VALUE FOR syntax]
        end
    end
    
    APP -->|SqlCommand + AddWithValue ⚠️| SP1 & SP2 & SP3 & SP4 & SP5 & SP6 & SP7
    SP1 & SP2 & SP3 & SP4 & SP5 & SP6 & SP7 --> T1
    T1 --> T2 & T3
    APP -.-> CONN
    SP3 --> SEQ
    
    style CONN fill:#e74c3c,color:#fff
    style SP5 fill:#ff9f43,color:#fff
    style APP fill:#ff9f43,color:#fff
```

### 4.2 Migration Intermediate: PostgreSQL Functions

```mermaid
graph TB
    subgraph "Application (Migration Bridge)"
        BRIDGE[Migration Validation Layer<br/>Npgsql / pgLoader]
    end
    
    subgraph "PostgreSQL (Azure Flexible Server)"
        subgraph "Tables (Translated Schema)"
            PT1[(catalog<br/>VARCHAR, BOOLEAN, NUMERIC)]
            PT2[(catalog_brand)]
            PT3[(catalog_type)]
        end
        
        subgraph "Functions (PL/pgSQL — Step 2)"
            PF1[SP1: get_catalog_items_paginated<br/>LIMIT/OFFSET, OUT param]
            PF2[SP2: get_catalog_item_by_id]
            PF3[SP3: create_catalog_item<br/>BEGIN...EXCEPTION, RAISE]
            PF4[SP4: update_catalog_item]
            PF5[SP5: update_inventory<br/>🟡 4 Business Rules Preserved<br/>SAVEPOINTS, BOOLEAN flags]
            PF6[SP6: delete_catalog_item]
            PF7A[SP7a: get_inventory_report_by_brand<br/>COUNT FILTER WHERE]
            PF7B[SP7b: get_inventory_reorder_items]
        end
        
        subgraph "Sequences"
            PSEQ[catalog_hilo<br/>catalog_brand_hilo<br/>catalog_type_hilo<br/>nextval syntax]
        end
    end
    
    BRIDGE -->|Npgsql parameterized| PF1 & PF2 & PF3 & PF4 & PF5 & PF6 & PF7A & PF7B
    PF1 & PF2 & PF3 & PF4 & PF5 & PF6 & PF7A & PF7B --> PT1
    PT1 --> PT2 & PT3
    PF3 --> PSEQ
    
    style PF5 fill:#f39c12,color:#fff
    style BRIDGE fill:#3498db,color:#fff
```

### 4.3 After: EF Core with Npgsql Provider (Modernized)

```mermaid
graph TB
    subgraph "Modernized Application (.NET 10)"
        subgraph "Controllers"
            CC[CatalogController]
            IC[InventoryController ✨]
        end
        
        subgraph "Domain + Services"
            CR[CatalogRepository<br/>async EF Core LINQ]
            IS[InventoryService<br/>C# Business Rules ✅]
            CI[CatalogItem.AdjustStock()<br/>4 Rules in C# ✅]
        end
        
        subgraph "Data Access"
            CTX[CatalogDbContext<br/>EF Core 10.x]
            CFG[appsettings.json<br/>DatabaseProvider switch]
        end
    end
    
    subgraph "Database (Switchable)"
        subgraph "Option A: SQL Server"
            SQL[(SQL Server<br/>via UseSqlServer)]
        end
        
        subgraph "Option B: PostgreSQL"
            PG[(Azure PG Flexible Server<br/>via UseNpgsql<br/>Entra ID Auth 🔒)]
        end
    end
    
    CC --> CR
    IC --> IS --> CI
    CR --> CTX
    IS --> CTX
    CTX -->|DatabaseProvider=SqlServer| SQL
    CTX -->|DatabaseProvider=PostgreSQL| PG
    CTX -.-> CFG
    
    style CI fill:#2ecc71,color:#fff
    style IS fill:#2ecc71,color:#fff
    style PG fill:#336791,color:#fff
    style SQL fill:#cc2927,color:#fff
    style CFG fill:#3498db,color:#fff
```

### 4.4 Data Migration Flow

```mermaid
flowchart TB
    subgraph "Source: SQL Server"
        SRC[(SQL Server DB<br/>3 Tables + 7 SPs<br/>+ 3 HiLo Sequences)]
    end
    
    subgraph "Migration Methods (Choose One or Both)"
        subgraph "Method A: pgLoader (Bulk Data)"
            PGL[pgLoader<br/>Open-source ETL tool]
            PGLCFG[pgloader.conf<br/>Type mapping rules<br/>NVARCHAR→TEXT<br/>BIT→BOOLEAN<br/>DECIMAL→NUMERIC]
        end
        
        subgraph "Method B: EF Core Migrations"
            EFM[dotnet ef migrations add<br/>InitialPostgreSQL]
            SEED[HasData Seed<br/>Already in CatalogDbContext]
        end
    end
    
    subgraph "Validation (Three Methods)"
        V1[✅ Method 1: MSSQL Extension<br/>mssql_list_tables<br/>mssql_run_query<br/>Source schema baseline]
        V2[✅ Method 2: EF Core Migrations<br/>Compare generated DDL<br/>Column types match<br/>Sequences created]
        V3[✅ Method 3: Unit Tests<br/>29 existing tests pass<br/>Business logic identical<br/>Both providers verified]
    end
    
    subgraph "Target: PostgreSQL"
        TGT[(Azure PG Flexible Server<br/>3 Tables + 3 Sequences<br/>No stored functions needed<br/>EF Core manages schema)]
    end
    
    SRC -->|Schema + Data| PGL
    PGL --> PGLCFG --> TGT
    SRC -.->|Schema only| EFM
    EFM --> SEED --> TGT
    TGT --> V1 & V2 & V3
    
    style SRC fill:#cc2927,color:#fff
    style TGT fill:#336791,color:#fff
    style PGL fill:#f39c12,color:#fff
    style V1 fill:#2ecc71,color:#fff
    style V2 fill:#2ecc71,color:#fff
    style V3 fill:#2ecc71,color:#fff
```

### 4.5 Dual Provider Configuration Flow

```mermaid
flowchart LR
    subgraph "Application Startup"
        CFG[appsettings.json<br/>DatabaseProvider key]
        PGM[Program.cs<br/>Conditional provider]
    end
    
    CFG -->|Read config| PGM
    
    PGM -->|"DatabaseProvider = SqlServer"| SQL_PATH
    PGM -->|"DatabaseProvider = PostgreSQL"| PG_PATH
    
    subgraph SQL_PATH["SQL Server Path"]
        USS[UseSqlServer<br/>+ EnableRetryOnFailure]
        HCSQL[HealthCheck: SqlServer]
    end
    
    subgraph PG_PATH["PostgreSQL Path"]
        UNP[UseNpgsql<br/>+ EnableRetryOnFailure]
        HCPG[HealthCheck: NpgSql]
    end
    
    SQL_PATH --> SQLDB[(SQL Server)]
    PG_PATH --> PGDB[(PostgreSQL)]
    
    style SQL_PATH fill:#cc2927,color:#fff
    style PG_PATH fill:#336791,color:#fff
```

### 4.6 Three-Step SP Transformation (Before → Intermediate → After)

```mermaid
flowchart LR
    subgraph "Step 1: T-SQL (Legacy)"
        direction TB
        SP1T["SP1: sp_GetCatalogItemsPaginated"]
        SP2T["SP2: sp_GetCatalogItemById"]
        SP3T["SP3: sp_CreateCatalogItem"]
        SP4T["SP4: sp_UpdateCatalogItem"]
        SP5T["SP5: sp_UpdateInventory<br/>🔴 Business Logic in SQL"]
        SP6T["SP6: sp_DeleteCatalogItem"]
        SP7T["SP7: sp_GetInventoryReport"]
    end
    
    subgraph "Step 2: PL/pgSQL (Bridge)"
        direction TB
        PF1T["SP1: get_catalog_items_paginated"]
        PF2T["SP2: get_catalog_item_by_id"]
        PF3T["SP3: create_catalog_item"]
        PF4T["SP4: update_catalog_item"]
        PF5T["SP5: update_inventory<br/>🟡 Same Rules, PG Syntax"]
        PF6T["SP6: delete_catalog_item"]
        PF7T["SP7: get_inventory_report_*<br/>(split into 2 functions)"]
    end
    
    subgraph "Step 3: C# EF Core (Modern)"
        direction TB
        EF1["CatalogRepository.GetItemsAsync()"]
        EF2["CatalogRepository.GetByIdAsync()"]
        EF3["CatalogRepository.AddAsync()"]
        EF4["CatalogRepository.UpdateAsync()"]
        EF5["CatalogItem.AdjustStock() ✅<br/>+ InventoryService"]
        EF6["CatalogRepository.DeleteAsync()"]
        EF7["InventoryService.GetReportAsync()"]
    end
    
    SP1T --> PF1T --> EF1
    SP2T --> PF2T --> EF2
    SP3T --> PF3T --> EF3
    SP4T --> PF4T --> EF4
    SP5T --> PF5T --> EF5
    SP6T --> PF6T --> EF6
    SP7T --> PF7T --> EF7
    
    style SP5T fill:#e74c3c,color:#fff
    style PF5T fill:#f39c12,color:#fff
    style EF5 fill:#2ecc71,color:#fff
```

---

## 5. Schema DDL Comparison

### Table: Catalog (Primary Entity)

| Column | T-SQL (SQL Server) | PL/pgSQL (PostgreSQL) | EF Core C# Type |
|---|---|---|---|
| `Id` | `INT IDENTITY(1,1)` | `INTEGER GENERATED ALWAYS AS IDENTITY` | `int` (UseHiLo) |
| `Name` | `NVARCHAR(50) NOT NULL` | `VARCHAR(50) NOT NULL` | `string` (MaxLength 50) |
| `Description` | `NVARCHAR(MAX) NULL` | `TEXT NULL` | `string?` |
| `Price` | `DECIMAL(18,2) NOT NULL` | `NUMERIC(18,2) NOT NULL` | `decimal` |
| `PictureFileName` | `NVARCHAR(MAX) NOT NULL` | `TEXT NOT NULL` | `string` |
| `CatalogTypeId` | `INT NOT NULL FK` | `INTEGER NOT NULL REFERENCES` | `int` (HasForeignKey) |
| `CatalogBrandId` | `INT NOT NULL FK` | `INTEGER NOT NULL REFERENCES` | `int` (HasForeignKey) |
| `AvailableStock` | `INT NOT NULL DEFAULT 0` | `INTEGER NOT NULL DEFAULT 0` | `int` |
| `RestockThreshold` | `INT NOT NULL DEFAULT 0` | `INTEGER NOT NULL DEFAULT 0` | `int` |
| `MaxStockThreshold` | `INT NOT NULL DEFAULT 0` | `INTEGER NOT NULL DEFAULT 0` | `int` |
| `OnReorder` | `BIT NOT NULL DEFAULT 0` | `BOOLEAN NOT NULL DEFAULT FALSE` | `bool` |

### Sequences

| T-SQL | PostgreSQL | EF Core |
|---|---|---|
| `CREATE SEQUENCE [dbo].[catalog_hilo] START WITH 1 INCREMENT BY 10` | `CREATE SEQUENCE catalog_hilo START WITH 1 INCREMENT BY 10` | `builder.HasSequence<long>("catalog_hilo").StartsAt(1).IncrementsBy(10)` |
| `NEXT VALUE FOR [dbo].[catalog_hilo]` | `nextval('catalog_hilo')` | `.UseHiLo("catalog_hilo")` (automatic) |

---

## 6. Data Migration Methods

### Method A: pgLoader (Recommended for Existing Production Data)

[pgLoader](https://pgloader.io/) is an open-source tool for bulk data migration from SQL Server to PostgreSQL with automatic type mapping.

**Configuration File (`pgloader.conf`):**

```ini
LOAD DATABASE
    FROM mssql://sa:***@localhost/Microsoft.eShopOnContainers.Services.CatalogDb
    INTO postgresql://eshop_user@localhost/eshop_catalog_db

WITH
    include drop,
    create tables,
    create indexes,
    reset sequences

SET
    maintenance_work_mem to '512MB',
    work_mem to '64MB'

CAST
    -- T-SQL → PG type mappings
    type nvarchar to text using remove-null-characters,
    type ntext    to text using remove-null-characters,
    type bit      to boolean using sql-server-bit-to-boolean,
    type decimal   to numeric,
    type datetime  to timestamptz,
    type datetime2 to timestamptz,
    type uniqueidentifier to uuid

BEFORE LOAD DO
    $$ CREATE SCHEMA IF NOT EXISTS public; $$;
```

**Run:**
```bash
pgloader pgloader.conf
```

### Method B: EF Core Migrations (Recommended for Greenfield / Dev Environments)

The modernized app uses EF Core Code First with `HasData()` seed data. Generating PostgreSQL migrations from the existing model:

```powershell
# Set connection string to PostgreSQL
$env:ConnectionStrings__CatalogDb = "Host=localhost;Database=eshop_catalog_db;Username=eshop_user;Password=***"

# Set provider
$env:DatabaseProvider = "PostgreSQL"

# Generate migration
dotnet ef migrations add InitialPostgreSQL `
    --project modernized/src/eShopModernized `
    --context CatalogDbContext

# Apply migration + seed data
dotnet ef database update `
    --project modernized/src/eShopModernized `
    --context CatalogDbContext
```

The generated migration will automatically:
- Create tables with PostgreSQL-native types (`text`, `boolean`, `numeric`)
- Create HiLo sequences
- Insert seed data (12 catalog items, 5 brands, 4 types)

---

## 7. Three Verification Methods

### Method 1: MSSQL Extension — Source Database Inspection

Use the **SQL Server Extension for VS Code** to inspect the source schema and establish a baseline:

```
Tools used:
  mssql_connect          → Connect to source SQL Server
  mssql_list_tables      → Enumerate all tables (Catalog, CatalogBrand, CatalogType)
  mssql_show_schema      → Get column definitions, types, constraints
  mssql_run_query        → Execute validation queries

Validation queries:
  1. SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
     FROM INFORMATION_SCHEMA.COLUMNS ORDER BY TABLE_NAME, ORDINAL_POSITION
  
  2. SELECT name, definition FROM sys.sql_modules
     WHERE object_id IN (SELECT object_id FROM sys.procedures)
  
  3. SELECT name, start_value, increment FROM sys.sequences
```

**Expected Results:**
- 3 tables with matching column counts
- 7 stored procedures with definitions
- 3 sequences (catalog_hilo, catalog_brand_hilo, catalog_type_hilo)

### Method 2: EF Core Migrations — Target Schema Validation

Compare the generated PostgreSQL DDL against the source SQL Server DDL:

```powershell
# Generate SQL script (without applying)
dotnet ef migrations script `
    --project modernized/src/eShopModernized `
    --context CatalogDbContext `
    --output pg_schema.sql

# Compare with source DDL
# Verify: column counts match, FK constraints match, sequences created
```

**Validation Checklist:**
- [ ] All 3 tables created with correct column types
- [ ] Foreign key constraints preserved (CatalogTypeId, CatalogBrandId)
- [ ] HiLo sequences created with START 1 INCREMENT 10
- [ ] Seed data row counts match (12 items, 5 brands, 4 types)
- [ ] Index definitions match (primary keys, FK indexes)

### Method 3: Unit Tests — Business Logic Equivalence

The existing 29 unit tests validate that business logic produces identical results regardless of database provider:

| Test Class | Tests | What It Validates |
|---|---|---|
| `CatalogItemInventoryTests` | 12 | AdjustStock() rules: negative prevention, max threshold, OnReorder flag |
| `InventoryServiceTests` | 5 | Service-level operations: UpdateStockAsync, GetInventoryReportAsync |
| `CatalogRepositoryTests` | 12 | CRUD operations: GetItems, GetById, Add, Update, Delete |

**Running tests against both providers:**

```powershell
# Tests use in-memory provider by default (provider-agnostic)
dotnet test modernized/tests/eShopModernized.Tests/ -c Release

# Optional: Integration tests against PostgreSQL
# Set environment variable to test against live PG instance
$env:TEST_DB_PROVIDER = "PostgreSQL"
$env:TEST_CONNECTION_STRING = "Host=localhost;Database=eshop_test;Username=test;Password=***"
dotnet test modernized/tests/eShopModernized.Tests/ -c Release --filter Category=Integration
```

**Key Business Rule Tests (SP5 equivalence):**
- `AdjustStock_SaleExceedsStock_ThrowsInvalidOperation` → Validates Rule 1
- `AdjustStock_RestockExceedsMaxThreshold_ThrowsInvalidOperation` → Validates Rule 2
- `AdjustStock_StockDropsBelowThreshold_SetsOnReorderTrue` → Validates Rule 3
- `AdjustStock_RestockAboveThreshold_SetsOnReorderFalse` → Validates Rule 3 (inverse)
- `AdjustStock_ValidSale_DecreasesStock` → Validates Rule 4 (atomic update)

---

## 8. Dual Database Provider Configuration

The modernized application supports both SQL Server and PostgreSQL through a single `DatabaseProvider` configuration key:

### appsettings.json

```json
{
  "DatabaseProvider": "PostgreSQL",
  "ConnectionStrings": {
    "CatalogDb": "Host=myserver.postgres.database.azure.com;Database=eshop_catalog_db;Username=eshop_app;SSL Mode=Require"
  }
}
```

### Program.cs (Conditional Provider)

```csharp
var dbProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "SqlServer";

builder.Services.AddDbContext<CatalogDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("CatalogDb")!;
    
    if (dbProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
    {
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
        });
    }
    else
    {
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });
    }
});
```

---

## 9. Azure Database for PostgreSQL Flexible Server

### Deployment Architecture

```mermaid
graph TB
    subgraph "Azure Cloud"
        subgraph "Compute"
            AS[Azure App Service<br/>.NET 10]
        end
        
        subgraph "Database"
            PG[Azure DB for PostgreSQL<br/>Flexible Server<br/>General Purpose D2s_v3]
            PGPOOL[PgBouncer<br/>Built-in Connection Pooling]
        end
        
        subgraph "Identity"
            MI[Managed Identity<br/>Entra ID Auth 🔒]
        end
        
        subgraph "Secrets"
            KV[Azure Key Vault<br/>Connection String]
        end
    end
    
    AS -->|Managed Identity| MI
    MI -->|Passwordless Auth| PG
    PG --> PGPOOL
    AS --> KV
    
    style MI fill:#2ecc71,color:#fff
    style PG fill:#336791,color:#fff
```

### Pricing Estimates

| Tier | SKU | vCores | RAM | Storage | Monthly Cost |
|---|---|---|---|---|---|
| **Dev/Test** | Burstable B1ms | 1 | 2 GB | 32 GB | ~$25/mo |
| **Production** | General Purpose D2s_v3 | 2 | 8 GB | 128 GB | ~$120/mo |
| **High Availability** | GP D2s_v3 + HA | 2 | 8 GB | 128 GB | ~$240/mo |

### Entra ID Passwordless Authentication

```json
{
  "ConnectionStrings": {
    "CatalogDb": "Host=eshop-pg.postgres.database.azure.com;Database=eshop_catalog_db;Username=eshop-app-identity;SSL Mode=Require"
  }
}
```

No password in the connection string — authentication flows through Managed Identity → Entra ID → PostgreSQL role mapping.

---

## 10. pgLoader vs EF Core Migrations Decision Guide

| Criteria | pgLoader | EF Core Migrations |
|---|---|---|
| **Best For** | Existing production data, large datasets | Greenfield environments, dev/test |
| **Schema Generation** | Auto-maps from source | Code-first from C# model |
| **Data Transfer** | Full data migration (bulk) | Seed data only (HasData) |
| **Type Mapping** | Configurable (`CAST` rules) | Automatic via Npgsql provider |
| **Stored Procedures** | Ignores (schema-only) | N/A (C# replaces SPs) |
| **Sequences** | Migrates and resets | Created by EF Core UseHiLo |
| **Rollback** | Drop target DB | `dotnet ef migrations remove` |
| **CI/CD Integration** | Script-based | `dotnet ef database update` |
| **Recommendation** | Production cutover night | Development + CI pipelines |

---

## 11. Migration Checklist

- [ ] **Schema translated**: All T-SQL DDL → PostgreSQL DDL verified (Section 5)
- [ ] **SP1-SP7 mapped**: All 7 stored procedures have PL/pgSQL translations (Section 1)
- [ ] **SP5 business rules**: All 4 inventory rules verified in PL/pgSQL AND C# (Section 3)
- [ ] **Dual provider configured**: `appsettings.json` + `Program.cs` support both SQL Server and PostgreSQL (Section 8)
- [ ] **Npgsql package added**: `eShopModernized.csproj` includes `Npgsql.EntityFrameworkCore.PostgreSQL` (csproj)
- [ ] **Health checks updated**: PostgreSQL health check registered alongside SQL Server (Program.cs)
- [ ] **pgLoader config**: Configuration file ready for production data migration (Section 6A)
- [ ] **EF migrations**: Initial PostgreSQL migration generated and tested (Section 6B)
- [ ] **Verification Method 1**: MSSQL Extension — source schema inspected and baselined
- [ ] **Verification Method 2**: EF Core migrations — target DDL compared against source
- [ ] **Verification Method 3**: Unit tests — all 29 tests pass (business logic unchanged)
- [ ] **Azure PG Flexible Server**: Deployment architecture documented with Entra ID auth (Section 9)
- [ ] **Key differences documented**: All 7 T-SQL → PL/pgSQL areas covered with examples (Section 2)

---

*Generated by Phase 4B: SQL Server → PostgreSQL Database Migration*
*Tools: MSSQL Extension, PostgreSQL Extension, pgLoader, EF Core Migrations, awesome-copilot MCP Server*
*Migration Date: March 10, 2026*
