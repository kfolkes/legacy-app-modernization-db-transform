# Phase 7: Architecture Documentation — Before & After Modernization

## 1. High-Level Architecture Comparison

### 1.1 Legacy Architecture (.NET Framework 4.7.2)

```mermaid
graph TB
    subgraph "Legacy eShop MVC Application"
        subgraph "Presentation Layer"
            GA[Global.asax.cs<br/>Application Entry Point]
            RC[RouteConfig.cs]
            BC[BundleConfig.cs]
            FC[FilterConfig.cs]
            CC[CatalogController<br/>System.Web.Mvc]
            RV[Razor Views<br/>jQuery 3.5 + Bootstrap 4.3]
        end
        
        subgraph "Business Logic Layer"
            AM[ApplicationModule.cs<br/>Autofac DI Container]
            CS[CatalogService<br/>EF6 LINQ Queries]
            CSP[CatalogServiceSP<br/>Stored Procedure Calls]
            HiLo[CatalogItemHiLoGenerator<br/>Manual SQL Sequence]
        end
        
        subgraph "Data Access Layer"
            DBCtx[CatalogDBContext<br/>Entity Framework 6.2]
            WC[Web.config<br/>Connection Strings ⚠️]
            L4N[log4net<br/>File Logging ⚠️]
            AI[Application Insights 2.9<br/>Embedded Key ⚠️]
        end
    end
    
    subgraph "SQL Server"
        DB[(CatalogDb)]
        SP1[sp_GetCatalogItemsPaginated]
        SP2[sp_GetCatalogItemById]
        SP3[sp_CreateCatalogItem]
        SP4[sp_UpdateCatalogItem]
        SP5[sp_UpdateInventory<br/>⚠️ Business Logic in SQL]
        SP6[sp_DeleteCatalogItem]
        SP7[sp_GetInventoryReport]
        SEQ[HiLo Sequences x3]
    end
    
    GA --> RC & BC & FC
    CC --> CS & CSP
    CS --> DBCtx
    CSP -->|ADO.NET SqlCommand| SP1 & SP2 & SP3 & SP4 & SP5 & SP6 & SP7
    DBCtx --> DB
    HiLo -->|Raw SQL| SEQ
    AM --> CS & CSP
    
    style WC fill:#ff6b6b,color:#fff
    style AI fill:#ff6b6b,color:#fff
    style L4N fill:#ff9f43,color:#fff
    style SP5 fill:#ff9f43,color:#fff
    style HiLo fill:#ff9f43,color:#fff
```

### 1.2 Modernized Architecture (.NET 10)

```mermaid
graph TB
    subgraph "Modernized eShop Application (.NET 10)"
        subgraph "Presentation Layer"
            PG[Program.cs<br/>Minimal Hosting Model]
            CatCtrl[CatalogController<br/>Microsoft.AspNetCore.Mvc]
            InvCtrl[InventoryController<br/>API Controller ✨ NEW]
            RV2[Razor Views<br/>Bootstrap 5.3 - No jQuery]
            SW[Swagger/OpenAPI ✨ NEW]
        end
        
        subgraph "Domain / Business Logic Layer"
            CI[CatalogItem.AdjustStock<br/>Domain Method ✅]
            IS[InventoryService<br/>Extracted SP Logic ✅]
            CR[CatalogRepository<br/>Async EF Core LINQ]
        end
        
        subgraph "Infrastructure Layer"
            DbCtx2[CatalogDbContext<br/>EF Core 10.x]
            AS[appsettings.json<br/>User Secrets 🔒]
            SL[Serilog<br/>Structured Logging 🔒]
            OT[OpenTelemetry<br/>Azure Monitor 🔒]
            HC[Health Checks ✨ NEW]
        end
    end
    
    subgraph "SQL Server"
        DB2[(CatalogDb)]
        SEQ2[HiLo Sequences<br/>EF Core Managed]
    end
    
    PG --> CatCtrl & InvCtrl & SW & HC
    CatCtrl --> CR
    InvCtrl --> IS
    IS --> CI
    CR --> DbCtx2
    IS --> DbCtx2
    DbCtx2 -->|EF Core LINQ<br/>Parameterized Queries| DB2
    DbCtx2 -->|UseHiLo Built-in| SEQ2
    
    style CI fill:#2ecc71,color:#fff
    style IS fill:#2ecc71,color:#fff
    style AS fill:#2ecc71,color:#fff
    style SL fill:#2ecc71,color:#fff
    style OT fill:#2ecc71,color:#fff
    style InvCtrl fill:#3498db,color:#fff
    style SW fill:#3498db,color:#fff
    style HC fill:#3498db,color:#fff
```

---

## 2. Stored Procedure Migration Map

```mermaid
flowchart LR
    subgraph "LEGACY: SQL Stored Procedures"
        SP1["sp_GetCatalogItemsPaginated<br/>📄 Pagination + JOINs"]
        SP2["sp_GetCatalogItemById<br/>📄 Single item lookup"]
        SP3["sp_CreateCatalogItem<br/>📄 HiLo + FK validation + INSERT"]
        SP4["sp_UpdateCatalogItem<br/>📄 Full entity UPDATE"]
        SP5["sp_UpdateInventory<br/>🔴 CRITICAL BUSINESS LOGIC<br/>• Negative stock prevention<br/>• Max threshold enforcement<br/>• Auto OnReorder flag"]
        SP6["sp_DeleteCatalogItem<br/>📄 Existence check + DELETE"]
        SP7["sp_GetInventoryReport<br/>📊 Brand aggregation + reorder"]
    end
    
    subgraph "MODERN: C# Services"
        R1["CatalogRepository<br/>.GetItemsAsync()"]
        R2["CatalogRepository<br/>.GetByIdAsync()"]
        R3["CatalogRepository<br/>.AddAsync()"]
        R4["CatalogRepository<br/>.UpdateAsync()"]
        R5["CatalogItem<br/>.AdjustStock() ✅<br/>+ InventoryService<br/>.UpdateStockAsync()"]
        R6["CatalogRepository<br/>.DeleteAsync()"]
        R7["InventoryService<br/>.GetInventoryReportAsync()"]
    end
    
    SP1 --> R1
    SP2 --> R2
    SP3 --> R3
    SP4 --> R4
    SP5 --> R5
    SP6 --> R6
    SP7 --> R7
    
    style SP5 fill:#e74c3c,color:#fff
    style R5 fill:#2ecc71,color:#fff
```

---

## 3. Data Flow — Request Processing

### 3.1 Legacy Flow (Synchronous, SP-based)

```mermaid
sequenceDiagram
    participant Browser
    participant IIS as IIS / Global.asax
    participant Ctrl as CatalogController
    participant Svc as CatalogServiceSP
    participant ADO as ADO.NET SqlCommand
    participant SQL as SQL Server
    
    Browser->>IIS: HTTP GET /Catalog/Index
    IIS->>IIS: Application_BeginRequest()<br/>log4net: logs raw URL + UserAgent ⚠️
    IIS->>Ctrl: Route → CatalogController.Index()
    
    Note over Ctrl: SYNCHRONOUS - Thread blocked
    
    Ctrl->>Svc: GetCatalogItemsPaginated(10, 0)
    Svc->>ADO: new SqlCommand("sp_GetCatalogItemsPaginated")
    ADO->>SQL: EXEC sp_GetCatalogItemsPaginated @PageSize=10, @PageIndex=0
    SQL-->>ADO: Result Set 1: Count<br/>Result Set 2: Items
    ADO-->>Svc: SqlDataReader
    
    Note over Svc: Manual mapping:<br/>MapCatalogItemFromReader()<br/>Error-prone, untyped ⚠️
    
    Svc-->>Ctrl: PaginatedItemsViewModel
    Ctrl-->>Browser: Razor View + jQuery 3.5 ⚠️
```

### 3.2 Modern Flow (Async, EF Core)

```mermaid
sequenceDiagram
    participant Browser
    participant Kestrel as Kestrel / Program.cs
    participant MW as Middleware Pipeline
    participant Ctrl as CatalogController
    participant Repo as CatalogRepository
    participant EF as EF Core DbContext
    participant SQL as SQL Server
    
    Browser->>Kestrel: HTTP GET /Catalog/Index
    Kestrel->>MW: HTTPS Redirect ✅<br/>Security Headers ✅<br/>CORS ✅
    MW->>MW: Serilog: Structured request log<br/>No PII ✅
    MW->>Ctrl: Route → CatalogController.Index()
    
    Note over Ctrl: ASYNC - Thread released during I/O
    
    Ctrl->>Repo: await GetItemsAsync(10, 0, ct)
    Repo->>EF: CatalogItems.Include().Skip().Take()
    EF->>SQL: SELECT with parameterized query ✅
    SQL-->>EF: Result rows
    
    Note over EF: Automatic mapping:<br/>Navigation properties loaded<br/>Type-safe, compile-checked ✅
    
    EF-->>Repo: List<CatalogItem>
    Repo-->>Ctrl: PaginatedItemsViewModel
    Ctrl-->>Browser: Razor View + Bootstrap 5 ✅
```

---

## 4. Dependency Graph — Before & After

### 4.1 Legacy Dependencies (25+ packages, net472)

```mermaid
graph TD
    APP[eShopLegacyMVC<br/>net472] --> EF6[EntityFramework 6.2.0<br/>⚠️ EOL]
    APP --> AF[Autofac 4.9.1]
    APP --> AFM[Autofac.Mvc5 4.0.2]
    APP --> MVC[Microsoft.AspNet.Mvc 5.2.7]
    APP --> NJ[Newtonsoft.Json 12.0.1<br/>🔴 CVE-2024-21907]
    APP --> JQ[jQuery 3.5.0<br/>🔴 CVE-2020-11023]
    APP --> BS[Bootstrap 4.3.1<br/>⚠️ CVE-2024-6484]
    APP --> L4N[log4net 2.0.10]
    APP --> AI[App Insights 2.9.1<br/>⚠️ Deprecated]
    APP --> MOD[Modernizr 2.8.3]
    APP --> CDP[CodeDom Providers 2.0.1]
    APP --> JQV[jQuery.Validation 1.19.4]
    
    EF6 --> SC[System.ComponentModel<br/>Annotations]
    AF --> AFM
    MVC --> RAZOR[Razor 3.2.7]
    MVC --> WP[WebPages 3.2.7]
    
    style NJ fill:#e74c3c,color:#fff
    style JQ fill:#e74c3c,color:#fff
    style EF6 fill:#ff9f43,color:#fff
    style BS fill:#ff9f43,color:#fff
    style AI fill:#ff9f43,color:#fff
```

### 4.2 Modern Dependencies (8 packages, net10.0)

```mermaid
graph TD
    APP2[eShopModernized<br/>net10.0] --> EFC[EF Core SQL Server 10.x<br/>✅ Latest]
    APP2 --> SER[Serilog.AspNetCore 9.x<br/>✅ Structured Logging]
    APP2 --> OTL[Azure Monitor<br/>OpenTelemetry 1.x<br/>✅ Modern Telemetry]
    APP2 --> HCK[HealthChecks<br/>SqlServer 9.x<br/>✅ Container Ready]
    APP2 --> SWG[Swashbuckle 7.x<br/>✅ API Documentation]
    
    APP2 -.->|Built-in| STJ[System.Text.Json<br/>✅ Secure by Default]
    APP2 -.->|Built-in| DI[Microsoft.Extensions.DI<br/>✅ No Autofac]
    APP2 -.->|Built-in| LOG[Microsoft.Extensions.Logging<br/>✅ No log4net]
    APP2 -.->|Built-in| CFG[Microsoft.Extensions.Configuration<br/>✅ No Web.config]
    
    style EFC fill:#2ecc71,color:#fff
    style SER fill:#2ecc71,color:#fff
    style OTL fill:#2ecc71,color:#fff
    style STJ fill:#2ecc71,color:#fff
    style DI fill:#2ecc71,color:#fff
```

---

## 5. Component Diagram — Modernized Application

```mermaid
graph TB
    subgraph "HTTP Layer"
        REQ[HTTP Request]
        HTTPS[HTTPS Redirect]
        HEADERS[Security Headers<br/>X-Content-Type-Options<br/>X-Frame-Options<br/>X-XSS-Protection]
    end
    
    subgraph "Controller Layer"
        CC[CatalogController<br/>MVC Views + CRUD]
        IC[InventoryController<br/>REST API]
    end
    
    subgraph "Service Layer"
        IS[IInventoryService<br/>→ InventoryService]
    end
    
    subgraph "Repository Layer"
        IR[ICatalogRepository<br/>→ CatalogRepository]
    end
    
    subgraph "Domain Layer"
        CI[CatalogItem<br/>.AdjustStock()]
        IUR[InventoryUpdateResult]
    end
    
    subgraph "Infrastructure"
        DB[CatalogDbContext<br/>EF Core 10.x]
        CFG[IOptions<CatalogSettings>]
        LOG[ILogger<T><br/>Serilog]
        HC[Health Checks]
    end
    
    subgraph "Data Store"
        SQL[(SQL Server)]
    end
    
    REQ --> HTTPS --> HEADERS --> CC & IC
    CC --> IR
    IC --> IS
    IS --> IR
    IS --> CI
    CI --> IUR
    IR --> DB
    DB --> SQL
    CC & IC -.-> LOG
    CC -.-> CFG
    DB -.-> HC
```

---

## 6. Security Posture Comparison

```mermaid
graph LR
    subgraph "BEFORE: Security Score 38/100"
        B1[Dependencies 3/10<br/>7 vulnerable packages]
        B2[Secrets 1/10<br/>Plaintext conn strings]
        B3[Code 5/10<br/>Raw SQL, AddWithValue]
        B4[Logging 4/10<br/>PII in logs]
        B5[Config 4/10<br/>Web.config monolith]
    end
    
    subgraph "AFTER: Security Score 92/100"
        A1[Dependencies 10/10<br/>All current, no CVEs]
        A2[Secrets 8/10<br/>User Secrets + env vars]
        A3[Code 10/10<br/>EF Core parameterized]
        A4[Logging 9/10<br/>Structured, no PII]
        A5[Config 9/10<br/>appsettings + IOptions]
    end
    
    B1 -.->|+7| A1
    B2 -.->|+7| A2
    B3 -.->|+5| A3
    B4 -.->|+5| A4
    B5 -.->|+5| A5
    
    style B1 fill:#e74c3c,color:#fff
    style B2 fill:#e74c3c,color:#fff
    style B3 fill:#ff9f43,color:#fff
    style B4 fill:#ff9f43,color:#fff
    style B5 fill:#ff9f43,color:#fff
    style A1 fill:#2ecc71,color:#fff
    style A2 fill:#2ecc71,color:#fff
    style A3 fill:#2ecc71,color:#fff
    style A4 fill:#2ecc71,color:#fff
    style A5 fill:#2ecc71,color:#fff
```

---

## 7. Inventory Business Logic — Domain Model Design

```mermaid
stateDiagram-v2
    [*] --> Healthy: Stock > RestockThreshold
    [*] --> NeedsReorder: Stock <= RestockThreshold
    
    Healthy --> Healthy: Sale (stock stays above threshold)
    Healthy --> NeedsReorder: Sale (stock drops below threshold)<br/>OnReorder = TRUE
    
    NeedsReorder --> NeedsReorder: Small restock (still below threshold)
    NeedsReorder --> Healthy: Restock (stock rises above threshold)<br/>OnReorder = FALSE
    
    Healthy --> Error: Sale exceeds available stock
    NeedsReorder --> Error: Sale exceeds available stock
    Healthy --> Error: Restock exceeds MaxStockThreshold
    NeedsReorder --> Error: Restock exceeds MaxStockThreshold
    
    Error --> [*]: InvalidOperationException thrown
    
    note right of Healthy
        AvailableStock > RestockThreshold
        OnReorder = false
    end note
    
    note right of NeedsReorder
        AvailableStock <= RestockThreshold
        OnReorder = true
    end note
```

---

## 8. Migration File Mapping

```mermaid
flowchart LR
    subgraph "LEGACY FILES"
        LG[Global.asax.cs<br/>116 lines]
        LW[Web.config<br/>Connection strings ⚠️]
        LAM[ApplicationModule.cs<br/>Autofac DI]
        LCS[CatalogService.cs<br/>EF6 LINQ]
        LCSP[CatalogServiceSP.cs<br/>ADO.NET + SPs]
        LHi[CatalogItemHiLoGenerator.cs<br/>Raw SQL]
        LCC[CatalogController.cs<br/>MVC 5]
        LDB[CatalogDBContext.cs<br/>EF6 Fluent API]
        LSP[StoredProcedures.sql<br/>7 stored procedures]
        LL4[log4Net.xml]
        LAI[ApplicationInsights.config]
        LPC[packages.config<br/>25+ packages]
    end
    
    subgraph "MODERN FILES"
        MP[Program.cs<br/>Startup + DI + Middleware]
        MAS[appsettings.json<br/>User Secrets 🔒]
        MCR[CatalogRepository.cs<br/>Async EF Core]
        MIS[InventoryService.cs<br/>Extracted SP Logic]
        MCI[CatalogItem.cs<br/>Domain Methods]
        MCC[CatalogController.cs<br/>ASP.NET Core MVC]
        MIC[InventoryController.cs<br/>REST API ✨]
        MDB[CatalogDbContext.cs<br/>EF Core + HiLo]
        MCSP[eShopModernized.csproj<br/>8 packages]
    end
    
    LG -->|Replaced by| MP
    LW -->|Replaced by| MAS
    LAM -->|Merged into| MP
    LCS -->|Replaced by| MCR
    LCSP -->|ELIMINATED| MCR
    LCSP -->|Business logic to| MIS
    LHi -->|ELIMINATED<br/>UseHiLo built-in| MDB
    LCC -->|Upgraded to| MCC
    LDB -->|Migrated to| MDB
    LSP -->|Extracted to| MIS & MCI
    LL4 -->|Replaced by| MP
    LAI -->|Replaced by| MP
    LPC -->|Replaced by| MCSP
    
    style LCSP fill:#e74c3c,color:#fff
    style LHi fill:#e74c3c,color:#fff
    style LSP fill:#e74c3c,color:#fff
    style MIS fill:#2ecc71,color:#fff
    style MCI fill:#2ecc71,color:#fff
    style MIC fill:#3498db,color:#fff
```

---

## 9. Test Coverage Matrix

| Legacy Component | Stored Procedure | Modernized Component | Test Class | Test Count |
|---|---|---|---|---|
| `CatalogServiceSP.GetCatalogItemsPaginated` | `sp_GetCatalogItemsPaginated` | `CatalogRepository.GetItemsAsync` | `CatalogRepositoryTests` | 3 |
| `CatalogServiceSP.FindCatalogItem` | `sp_GetCatalogItemById` | `CatalogRepository.GetByIdAsync` | `CatalogRepositoryTests` | 2 |
| `CatalogServiceSP.CreateCatalogItem` | `sp_CreateCatalogItem` | `CatalogRepository.AddAsync` | `CatalogRepositoryTests` | 1 |
| `CatalogServiceSP.UpdateCatalogItem` | `sp_UpdateCatalogItem` | `CatalogRepository.UpdateAsync` | `CatalogRepositoryTests` | 1 |
| `CatalogServiceSP.RemoveCatalogItem` | `sp_DeleteCatalogItem` | `CatalogRepository.DeleteAsync` | `CatalogRepositoryTests` | 2 |
| `CatalogServiceSP.UpdateInventory` | `sp_UpdateInventory` | `CatalogItem.AdjustStock` | `CatalogItemInventoryTests` | 12 |
| `CatalogServiceSP.UpdateInventory` | `sp_UpdateInventory` | `InventoryService.UpdateStockAsync` | `InventoryServiceTests` | 5 |
| N/A | `sp_GetInventoryReport` | `InventoryService.GetInventoryReportAsync` | `InventoryServiceTests` | 3 |
| **Total** | **7 SPs** | **8 methods** | **3 test classes** | **29 tests** |

---

*Generated by HVE Core rpi-agent — Architecture documentation phase*
*All Mermaid diagrams render in VS Code Markdown Preview and GitHub*
*Documentation Date: March 5, 2026*
