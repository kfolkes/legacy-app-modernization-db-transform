# Phase 3: .NET 10 Modernization Plan with Integrated Security Remediation

## Overview

This plan merges the Phase 1 Legacy Assessment with the Phase 2 Security Baseline to create a unified modernization roadmap. Each step identifies which security findings it resolves, ensuring security improvements are built into the upgrade rather than bolted on afterward.

**Source:** HVE Core `task-planner` agent + `dotnet-upgrade` skill from awesome-copilot  
**Methodology:** Research → Plan → Implement (RPI)

---

## Step 1: Project File Conversion

**Duration:** 1 hour | **Risk:** Low

### Actions:
1. Convert `eShopLegacyMVC.csproj` from legacy XML format to SDK-style `.csproj`
2. Remove `packages.config` — convert all entries to `<PackageReference>`
3. Set `<TargetFramework>net10.0</TargetFramework>`
4. Remove implicit packages: `Microsoft.CodeDom.Providers.DotNetCompilerPlatform`, `Microsoft.Net.Compilers`
5. Remove `AssemblyInfo.cs` (SDK-style generates it)

### Security Fixes:
- **Resolves DC-7:** Removes unnecessary `Microsoft.CodeDom.Providers.DotNetCompilerPlatform` (reduces attack surface)

### Output:
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

---

## Step 2: NuGet Package Migration

**Duration:** 2 hours | **Risk:** Medium

### Package Replacement Matrix:

| Remove | Add | Version | Security Fix |
|---|---|---|---|
| EntityFramework 6.2.0 | Microsoft.EntityFrameworkCore.SqlServer | 10.x | Resolves DC-4 (EOL) |
| — | Microsoft.EntityFrameworkCore.Design | 10.x | — |
| Autofac 4.9.1 + Autofac.Mvc5 | *(built-in DI)* | — | Reduced dependency surface |
| Newtonsoft.Json 12.0.1 | *(System.Text.Json built-in)* | — | **Resolves DC-3** (DoS CVE) |
| log4net 2.0.10 | Serilog.AspNetCore | 9.x | Structured logging, PII scrubbing |
| Microsoft.ApplicationInsights.Web 2.9.1 | Azure.Monitor.OpenTelemetry.AspNetCore | 1.x | **Resolves DC-5** (EOL SDK) |
| jQuery 3.5.0 | *(remove or upgrade to 3.7+)* | — | **Resolves DC-1, DC-2** (XSS CVEs) |
| bootstrap 4.3.1 | bootstrap 5.3+ via npm/CDN | — | **Resolves DC-6** (XSS) |
| Modernizr 2.8.3 | *(remove)* | — | Unnecessary dependency |
| jQuery.Validation 1.19.4 | *(remove — use unobtrusive validation)* | — | — |

---

## Step 3: Startup & Configuration Migration

**Duration:** 3 hours | **Risk:** High

### Actions:
1. **Delete** `Global.asax` + `Global.asax.cs`
2. **Create** `Program.cs` with minimal hosting model:
   ```csharp
   var builder = WebApplication.CreateBuilder(args);
   // Service registration (replaces Autofac ApplicationModule)
   // Middleware pipeline (replaces App_Start/*.cs)
   ```
3. **Migrate** `Web.config` → `appsettings.json` + `appsettings.Development.json`
4. **Move** connection strings to User Secrets (development) and environment variables (production)
5. **Delete** `App_Start/` folder — integrate into `Program.cs`
6. **Migrate** `ApplicationInsights.config` → OpenTelemetry configuration in `Program.cs`

### Security Fixes:
- **Resolves GR-3, TR-1:** Connection strings removed from source control → User Secrets + env vars
- **Resolves TR-2:** App Insights key → Managed Identity connection string
- **Resolves GR-4:** Config parsing → `IOptions<T>` with validation

---

## Step 4: Dependency Injection Migration

**Duration:** 1 hour | **Risk:** Medium

### Actions:
1. **Remove** `Modules/ApplicationModule.cs` (Autofac module)
2. **Register** services in `Program.cs`:
   ```csharp
   builder.Services.AddScoped<ICatalogRepository, CatalogRepository>();
   builder.Services.AddScoped<IInventoryService, InventoryService>();
   builder.Services.AddDbContext<CatalogDbContext>(options => ...);
   ```
3. **Map** Autofac lifetimes → built-in DI scopes:
   - `InstancePerLifetimeScope` → `Scoped`
   - `SingleInstance` → `Singleton`

---

## Step 5: Logging Migration

**Duration:** 1 hour | **Risk:** Low

### Actions:
1. **Remove** `log4Net.xml` configuration file
2. **Replace** all `static readonly ILog _log = LogManager.GetLogger(...)` with DI-injected `ILogger<T>`
3. **Configure** Serilog with structured logging in `Program.cs`
4. **Add** log scrubbing for PII (URLs, User-Agents)
5. **Replace** string interpolation logging with message templates:
   ```csharp
   // Before: _log.Info($"Now loading... /Catalog/Index?pageSize={pageSize}");  
   // After:  _logger.LogInformation("Loading catalog index. PageSize={PageSize}, PageIndex={PageIndex}", pageSize, pageIndex);
   ```

### Security Fixes:
- **Resolves TR-3:** Log PII exposure → structured logging with data masking

---

## Step 6: EF6 → EF Core Migration

**Duration:** 4 hours | **Risk:** High

### Actions:
1. **Rename** `CatalogDBContext` → `CatalogDbContext` (naming convention)
2. **Convert** Fluent API from EF6 to EF Core syntax:
   - `DbModelBuilder` → `ModelBuilder`
   - `EntityTypeConfiguration<T>` → `IEntityTypeConfiguration<T>`
   - `HasRequired<T>()` → `HasOne<T>().WithMany().HasForeignKey()`
3. **Migrate** HiLo sequences to EF Core built-in:
   ```csharp
   modelBuilder.HasSequence<long>("catalog_hilo").StartsAt(1).IncrementsBy(10);
   modelBuilder.Entity<CatalogItem>().Property(o => o.Id).UseHiLo("catalog_hilo");
   ```
4. **Create** EF Core migrations from existing schema
5. **Convert** all data access to async

### Security Fixes:
- **Resolves GR-1:** Raw `SqlQuery<Int64>()` → EF Core `UseHiLo()` (no raw SQL)

---

## Step 6.5: Database Provider Migration (SQL Server → PostgreSQL)

**Duration:** 3 hours | **Risk:** Medium

### Actions:
1. **Add** `Npgsql.EntityFrameworkCore.PostgreSQL` and `AspNetCore.HealthChecks.NpgSql` to the project
2. **Create** `appsettings.PostgreSQL.json` with Azure PG Flexible Server connection (Entra ID passwordless auth)
3. **Add** `DatabaseProvider` configuration key to `appsettings.json` (values: `SqlServer` | `PostgreSQL`)
4. **Update** `Program.cs` with conditional `UseSqlServer()` / `UseNpgsql()` based on `DatabaseProvider` config
5. **Update** health checks for dual provider support (SqlServer or NpgSql)
6. **Create** PL/pgSQL function translations of all 7 stored procedures (migration intermediate validation)
7. **Generate** EF Core migration for PostgreSQL target
8. **Document** all T-SQL → PL/pgSQL syntax differences (19 areas identified)
9. **Create** pgLoader configuration for production data migration

### Security Fixes:
- **Azure PG Flexible Server** uses Entra ID Managed Identity authentication (passwordless — no credentials in config)
- **Connection string** contains no password — authentication flows through Managed Identity → Entra ID → PostgreSQL role mapping

### Verification:
- All 29 existing unit tests pass on both providers (business logic is provider-agnostic)
- EF Core generated DDL matches expected PostgreSQL schema (3 tables, 3 sequences, all FKs preserved)
- pgLoader type mappings validated (NVARCHAR→TEXT, BIT→BOOLEAN, DECIMAL→NUMERIC)

### Output:
- `docs/04b-database-migration.md` — Full evidence doc with labeled SP mapping, Mermaid diagrams, difference tables
- `appsettings.PostgreSQL.json` — Azure PG Flexible Server connection config
- `StoredFunctions_PostgreSQL.sql` — PL/pgSQL translations (8 functions from 7 SPs — SP7 split into 2)

---

## Step 7: Stored Procedure Business Logic Extraction

**Duration:** 4 hours | **Risk:** **Critical** — Must preserve all business rules

### Actions:
1. **Create** `ICatalogRepository` interface (async methods)
2. **Create** `CatalogRepository` implementing EF Core data access
3. **Create** `IInventoryService` with extracted business rules from `sp_UpdateInventory`
4. **Create** `InventoryService` with C# implementation of:
   - Negative stock prevention
   - Max stock threshold enforcement
   - Auto OnReorder flag management
   - Atomic transactional operations
5. **Delete** `CatalogServiceSP.cs` — all SP logic now in C#
6. **Map** each SP to its EF Core replacement (see Phase 1 map)

### Security Fixes:
- **Resolves GR-2:** `AddWithValue()` pattern eliminated — no more raw ADO.NET

### Verification:
- Write unit tests for ALL business rules BEFORE migration (Phase 6)
- Compare SP output vs. C# service output for same inputs

---

## Step 8: Controller Modernization

**Duration:** 2 hours | **Risk:** Medium

### Actions:
1. **Convert** `CatalogController` from MVC 5 to ASP.NET Core MVC
2. **Remove** `System.Web.Mvc` → `Microsoft.AspNetCore.Mvc`
3. **Add** `[ApiController]` attribute for API endpoints
4. **Replace** `HttpStatusCodeResult` → `BadRequest()`, `NotFound()`, etc.
5. **Replace** `ViewBag` → strongly-typed `ViewModels`
6. **Replace** `[Bind(Include=...)]` → dedicated input DTOs
7. **Convert** all actions to async
8. **Add** model validation with `[Required]`, `[Range]` on DTOs
9. **Add** CORS policy in `Program.cs`

---

## Step 9: UI Modernization

**Duration:** 2 hours | **Risk:** Low

### Actions:
1. **Remove** jQuery and jQuery.Validation
2. **Upgrade** Bootstrap 4.3.1 → 5.3+ (drop jQuery dependency)
3. **Remove** Modernizr
4. **Update** Razor views for ASP.NET Core tag helpers
5. **Replace** `@Html.ActionLink` → `<a asp-action="...">`
6. **Replace** `@Scripts.Render` → `<script>` tags with modern bundling

---

## Step 10: Security Hardening

**Duration:** 2 hours | **Risk:** Medium

### Actions:
1. **Add** HTTPS redirection and HSTS middleware
2. **Add** anti-forgery token configuration for ASP.NET Core
3. **Add** rate limiting middleware
4. **Add** request size limits
5. **Configure** `System.Text.Json` with `MaxDepth` limit
6. **Add** security headers (CSP, X-Frame-Options, etc.)
7. **Add** health check endpoints (`/health`, `/ready`)

---

## Step 11: Async Conversion

**Duration:** 2 hours | **Risk:** Medium

### Actions:
1. **Convert** `ICatalogService` → `ICatalogRepository` with all async methods
2. **Convert** all `ToList()` → `ToListAsync()`
3. **Convert** `SaveChanges()` → `SaveChangesAsync()`
4. **Convert** `FirstOrDefault()` → `FirstOrDefaultAsync()`
5. **Update** controller actions to `async Task<IActionResult>`
6. **Add** `CancellationToken` parameters throughout

---

## Modernization Timeline

```
Week 1: Steps 1-4 (Project, NuGet, Startup, DI)
         → Resolves 5 security findings (DC-3, DC-4, DC-5, DC-7, GR-4)
         → App builds on .NET 10 but doesn't run yet

Week 2: Steps 5-7 (Logging, EF Core, DB Provider Migration, SP extraction)
         → Resolves 4 security findings (GR-1, GR-2, GR-3, TR-1, TR-2, TR-3)
         → Data access layer fully modernized; dual DB provider (SQL Server + PostgreSQL) configured

Week 3: Steps 8-11 (Controllers, UI, Security, Async)
         → Resolves 3 security findings (DC-1, DC-2, DC-6)
         → App fully functional on .NET 10

Week 4: Testing, documentation, deployment planning (Phases 5-8)
```

---

## Risk Mitigation Checkpoints

| Checkpoint | After Step | Validation |
|---|---|---|
| Build passes | Step 2 | `dotnet build` succeeds |
| Config loads | Step 3 | `appsettings.json` reads correctly |
| DI resolves | Step 4 | All services inject without error |
| DB connects | Step 6 | EF Core migrations apply, seed data loads |
| DB provider switch works | Step 6.5 | App starts with both `SqlServer` and `PostgreSQL` providers |
| Business logic preserved | Step 7 | Unit tests pass for all SP business rules |
| UI renders | Step 9 | All views display correctly |
| Security scan clean | Step 10 | sec-check re-scan shows 0 critical/high |
| All tests pass | Step 11 | `dotnet test` → all green |

---

*Generated by HVE Core task-planner agent — Plan phase of RPI methodology*
*Informed by: docs/01-legacy-assessment.md + docs/02-security-baseline.md*
*Plan Date: March 5, 2026*
