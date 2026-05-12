# Phase 1: Legacy Application Assessment & Business Logic Documentation

## Executive Summary

This document provides a comprehensive assessment of the **eShop Legacy MVC Application**, a .NET Framework 4.7.2 ASP.NET MVC 5 application that manages a product catalog with inventory tracking. The application uses Entity Framework 6.2.0 and stored procedures for data access against SQL Server, with Autofac dependency injection and log4net logging.

**Target State:** Modernize to **.NET 10** with ASP.NET Core, EF Core, built-in DI, and modern security practices.

---

## 1. Application Inventory

### 1.1 Technology Stack

| Component | Current (Legacy) | Target (.NET 10) |
|---|---|---|
| **Framework** | .NET Framework 4.7.2 | .NET 10 |
| **Web Framework** | ASP.NET MVC 5.2.7 | ASP.NET Core MVC / Minimal APIs |
| **ORM** | Entity Framework 6.2.0 | EF Core 10.x |
| **DI Container** | Autofac 4.9.1 | Microsoft.Extensions.DependencyInjection |
| **Logging** | log4net 2.0.10 | Microsoft.Extensions.Logging + Serilog |
| **Telemetry** | Application Insights 2.9.1 | OpenTelemetry + Azure Monitor |
| **JSON** | Newtonsoft.Json 12.0.1 | System.Text.Json |
| **UI Framework** | Bootstrap 4.3.1 + jQuery 3.5.0 | Bootstrap 5.x (no jQuery dependency) |
| **Project Format** | Legacy .csproj + packages.config | SDK-style .csproj with PackageReference |
| **Configuration** | Web.config + ConfigurationManager | appsettings.json + IConfiguration |
| **Startup** | Global.asax.cs | Program.cs (Minimal Hosting) |
| **Database** | SQL Server (LocalDB) | SQL Server / Azure SQL |

### 1.2 Project Structure

```
eShopLegacyMVCSolution/
├── src/eShopLegacyMVC/
│   ├── App_Start/                    # MVC startup configuration
│   │   ├── BundleConfig.cs           # CSS/JS bundling
│   │   ├── FilterConfig.cs           # Global MVC filters
│   │   ├── RouteConfig.cs            # URL routing
│   │   └── WebApiConfig.cs           # Web API configuration
│   ├── Controllers/
│   │   ├── CatalogController.cs      # Main CRUD controller (166 lines)
│   │   ├── PicController.cs          # Image serving controller
│   │   └── Api/WebApi/               # REST API controllers
│   ├── Models/
│   │   ├── CatalogItem.cs            # Product entity (64 lines)
│   │   ├── CatalogBrand.cs           # Brand lookup entity
│   │   ├── CatalogType.cs            # Category lookup entity
│   │   ├── CatalogDBContext.cs       # EF6 DbContext with Fluent API (94 lines)
│   │   ├── CatalogItemHiLoGenerator.cs # SQL Sequence-based ID generator
│   │   └── Infrastructure/
│   │       ├── CatalogDBInitializer.cs   # Database seeder
│   │       ├── PreconfiguredData.cs      # Seed data
│   │       ├── StoredProcedures.sql      # 7 stored procedures
│   │       └── *.Sequence.sql            # HiLo sequences (3 files)
│   ├── Services/
│   │   ├── ICatalogService.cs        # Service interface (7 methods)
│   │   ├── CatalogService.cs         # EF-backed implementation
│   │   ├── CatalogServiceSP.cs       # Stored procedure implementation
│   │   └── CatalogServiceMock.cs     # In-memory mock
│   ├── Modules/
│   │   └── ApplicationModule.cs      # Autofac DI registration
│   ├── Views/                        # Razor views (CRUD pages)
│   ├── ViewModel/
│   │   └── PaginatedItemsViewModel.cs # Pagination model
│   ├── Global.asax.cs                # Application entry point (116 lines)
│   ├── Web.config                    # Configuration with connection strings
│   ├── packages.config               # NuGet dependencies (30+ packages)
│   └── log4Net.xml                   # Logging configuration
```

---

## 2. Database Schema & Stored Procedures

### 2.1 Database Tables

```
┌─────────────────────────────────────────────────┐
│                   Catalog                        │
├─────────────────────────────────────────────────┤
│ Id (int, PK, no auto-increment - HiLo)          │
│ Name (nvarchar(50), required)                    │
│ Description (nvarchar(max), nullable)            │
│ Price (decimal(18,2), required)                  │
│ PictureFileName (nvarchar(max), required)        │
│ CatalogTypeId (int, FK → CatalogType)            │
│ CatalogBrandId (int, FK → CatalogBrand)          │
│ AvailableStock (int)                             │
│ RestockThreshold (int)                           │
│ MaxStockThreshold (int)                          │
│ OnReorder (bit)                                  │
└─────────────────────────────────────────────────┘
          │                    │
          ▼                    ▼
┌──────────────────┐  ┌──────────────────┐
│   CatalogType    │  │   CatalogBrand   │
├──────────────────┤  ├──────────────────┤
│ Id (int, PK)     │  │ Id (int, PK)     │
│ Type (nvarchar   │  │ Brand (nvarchar  │
│      (100))      │  │       (100))     │
└──────────────────┘  └──────────────────┘
```

### 2.2 SQL Sequences (HiLo Pattern)

| Sequence | Type | Start | Increment | Purpose |
|---|---|---|---|---|
| `catalog_hilo` | BIGINT | 1 | 10 | Catalog item ID generation |
| `catalog_brand_hilo` | BIGINT | 1 | 10 | Brand ID generation |
| `catalog_type_hilo` | BIGINT | 1 | 10 | Type ID generation |

### 2.3 Stored Procedures Inventory

| Stored Procedure | Parameters | Business Logic | Complexity |
|---|---|---|---|
| `sp_GetCatalogItemsPaginated` | @PageSize, @PageIndex | Paginated query with brand/type JOINs, OFFSET/FETCH | Medium |
| `sp_GetCatalogItemById` | @Id | Single item lookup with JOINs | Low |
| `sp_CreateCatalogItem` | 10 input + @NewId OUTPUT | HiLo ID gen, FK validation, default PictureFileName | High |
| `sp_UpdateCatalogItem` | 11 input params | Existence validation, full entity update | Medium |
| `sp_UpdateInventory` | @Id, @QuantityChange, 2 OUTPUT | **Core business logic**: negative stock prevention, max threshold enforcement, auto OnReorder flag | **Critical** |
| `sp_DeleteCatalogItem` | @Id | Existence validation, hard delete | Low |
| `sp_GetInventoryReport` | None | Aggregate reporting by brand, reorder identification | Medium |

---

## 3. Business Logic Map — Stored Procedure to Future State

### 3.1 SP → EF Core Migration Map

| Legacy (Stored Procedure) | Business Logic Embedded | Future State (.NET 10) | Migration Strategy |
|---|---|---|---|
| **sp_GetCatalogItemsPaginated** | Pagination with OFFSET/FETCH, multi-table JOIN | `ICatalogRepository.GetItemsAsync()` using EF Core LINQ with `.Include()`, `.Skip()`, `.Take()`, `ToListAsync()` | Direct LINQ translation |
| **sp_GetCatalogItemById** | Single item JOIN query | `ICatalogRepository.GetByIdAsync()` with `.Include()` navigation properties | Direct LINQ translation |
| **sp_CreateCatalogItem** | FK validation, HiLo sequence ID gen, default picture | `ICatalogRepository.AddAsync()` — EF Core handles FK validation via model config; use `UseHiLo()` for sequences; default value in entity constructor | Structural change — extract validation to domain service |
| **sp_UpdateCatalogItem** | Existence check, full entity update | `ICatalogRepository.UpdateAsync()` with EF Core change tracker (`DbContext.Update()`) | Direct migration, add concurrency token |
| **sp_UpdateInventory** | Negative stock prevention, max threshold check, auto OnReorder flag | **`InventoryService.UpdateStockAsync()`** — Extract all rules to C# domain service with FluentValidation; unit-testable without DB | **Critical extraction** — moves business logic from SQL to C# |
| **sp_DeleteCatalogItem** | Existence check, hard delete | `ICatalogRepository.DeleteAsync()` — consider adding soft delete with `IsDeleted` flag | Enhancement opportunity |
| **sp_GetInventoryReport** | GROUP BY aggregation, reorder analysis | `IReportingService.GetInventoryReportAsync()` — EF Core LINQ GroupBy with projections | Consider keeping as raw SQL for perf |

### 3.2 Business Rules to Extract from SQL

These business rules are currently embedded in `sp_UpdateInventory` and MUST be preserved in the modernized application:

```
RULE 1: Stock cannot go negative
  IF (CurrentStock + QuantityChange) < 0 → REJECT with error

RULE 2: Stock cannot exceed maximum threshold
  IF (NewStock > MaxStockThreshold) AND (MaxStockThreshold > 0) → REJECT with error

RULE 3: Auto-reorder flag management
  IF (NewStock <= RestockThreshold) → SET OnReorder = TRUE
  IF (NewStock > RestockThreshold) → SET OnReorder = FALSE

RULE 4: Atomic inventory operations
  All stock changes MUST be transactional (BEGIN TRAN / COMMIT / ROLLBACK)
```

---

## 4. NuGet Dependency Analysis

### 4.1 Packages Requiring Migration

| Package | Legacy Version | .NET 10 Replacement | Action |
|---|---|---|---|
| EntityFramework | 6.2.0 | Microsoft.EntityFrameworkCore.SqlServer 10.x | **Replace** — major API changes |
| Autofac | 4.9.1 | Microsoft.Extensions.DependencyInjection (built-in) | **Remove** — use built-in DI |
| Autofac.Mvc5 | 4.0.2 | N/A | **Remove** |
| Microsoft.AspNet.Mvc | 5.2.7 | Microsoft.AspNetCore.Mvc (built-in) | **Replace** — part of framework |
| log4net | 2.0.10 | Microsoft.Extensions.Logging + Serilog | **Replace** |
| Microsoft.ApplicationInsights.Web | 2.9.1 | Azure.Monitor.OpenTelemetry.AspNetCore | **Replace** |
| Newtonsoft.Json | 12.0.1 | System.Text.Json (built-in) | **Replace** — unless complex serialization needed |
| jQuery | 3.5.0 | **Remove or upgrade** — CVE-2020-11023 | **Security fix** |
| jQuery.Validation | 1.19.4 | Client-side via unobtrusive validation or remove | **Remove** |
| Modernizr | 2.8.3 | Remove — browser feature detection no longer needed | **Remove** |
| Microsoft.CodeDom.Providers.DotNetCompilerPlatform | 2.0.1 | N/A — Roslyn is built-in | **Remove** |
| Microsoft.Net.Compilers | 2.10.0 | N/A — SDK includes compiler | **Remove** |

### 4.2 Packages with Known CVEs

| Package | Version | CVE | Severity | Resolution |
|---|---|---|---|---|
| jQuery | 3.5.0 | CVE-2020-11023 | Medium | XSS via `.html()` — upgrade to 3.7+ or remove |
| Newtonsoft.Json | 12.0.1 | Potential deserialization attacks | Medium | Switch to System.Text.Json or upgrade |
| Microsoft.ApplicationInsights | 2.9.1 | Multiple deprecated APIs | Low | Replace with OpenTelemetry |

---

## 5. Legacy Patterns Requiring Modernization

### 5.1 Anti-Patterns Identified

| Pattern | Location | Issue | Modern Replacement |
|---|---|---|---|
| **Global.asax entry point** | `Global.asax.cs` | Non-standard startup, no host builder | `Program.cs` with `WebApplication.CreateBuilder()` |
| **Synchronous data access** | `CatalogService.cs` | All DB calls are sync (`ToList()`, `SaveChanges()`) | Async/await throughout (`ToListAsync()`) |
| **Manual SP data mapping** | `CatalogServiceSP.cs` | Hand-coded SqlDataReader → object mapping | EF Core automatic mapping or Dapper |
| **Static connection strings** | `Web.config` | Hardcoded in config file | User Secrets + Azure Key Vault |
| **ViewBag for dropdown data** | `CatalogController.cs` | Untyped dynamic object for select lists | Strongly-typed view models |
| **[Bind] attribute** | `CatalogController.cs` | Over-posting prevention via include lists | Input DTOs / command objects |
| **packages.config** | Root | Legacy NuGet format, no transitive deps | SDK-style .csproj with `<PackageReference>` |
| **HiLo manual implementation** | `CatalogItemHiLoGenerator.cs` | Custom lock-based sequence handling | EF Core built-in `UseHiLo()` |
| **ConfigurationManager** | Throughout | Static config access | `IConfiguration` / `IOptions<T>` pattern |
| **log4net static loggers** | `CatalogController.cs` | Per-class static `ILog` instances | DI-injected `ILogger<T>` |

### 5.2 Architecture Concerns

1. **No separation of concerns** — Controller directly calls Service with no domain layer
2. **Business logic in SQL** — Inventory rules in stored procedures, not unit-testable
3. **No API versioning** — REST endpoints have no version management
4. **No health checks** — No readiness/liveness probes for container deployment
5. **No structured logging** — log4net uses string interpolation, not structured data
6. **No middleware pipeline** — Request handling is in `Application_BeginRequest`
7. **No CORS configuration** — No cross-origin support
8. **Session state dependency** — `Session_Start` tracks machine name and session time

---

## 6. Risk Assessment

| Risk | Probability | Impact | Mitigation |
|---|---|---|---|
| EF6 → EF Core breaking changes in Fluent API | High | Medium | Test each entity mapping individually |
| Stored procedure business logic regression | High | **Critical** | Write unit tests for all SP business rules BEFORE migration |
| Autofac → built-in DI scope differences | Medium | Medium | Map lifetime scopes carefully (InstancePerLifetimeScope → Scoped) |
| jQuery removal breaks client validation | Medium | Low | Test all forms after removing jQuery |
| HiLo sequence migration | Medium | Medium | Verify sequence values carry over during DB migration |
| Connection string exposure during migration | Low | High | Use User Secrets from day 1 |

---

## 7. Recommended Migration Sequence

```
1. Project file conversion (packages.config → SDK-style)
2. NuGet package migration (update/replace all packages)
3. Startup modernization (Global.asax → Program.cs)
4. DI migration (Autofac → built-in)
5. Configuration migration (Web.config → appsettings.json)
6. Logging migration (log4net → ILogger<T>)
7. EF6 → EF Core (DbContext, Fluent API, migrations)
8. SP business logic extraction → domain services
9. Controller modernization (MVC 5 → ASP.NET Core MVC)
10. Security hardening (fix CVEs, add auth, CORS)
11. Async conversion (sync → async/await throughout)
12. UI modernization (remove jQuery, upgrade Bootstrap)
13. Testing (unit + integration tests)
14. Documentation & deployment planning
```

---

*Generated by HVE Core task-researcher agent — Research phase of RPI methodology*
*Assessment Date: March 5, 2026*
