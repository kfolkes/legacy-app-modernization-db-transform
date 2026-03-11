# Phase 8: Deployment & Migration Planning — PM Agent Report

## Executive Summary

This report evaluates four deployment strategies for the modernized eShop .NET 10 application, providing decision criteria, migration roadmaps, and cost frameworks for each option. The analysis considers both on-premises and Azure cloud deployments, with a special focus on microservices decomposition potential.

---

## Deployment Option Analysis

### Option A: On-Premises — IIS on Windows Server

```mermaid
graph TB
    subgraph "On-Premises Infrastructure"
        LB[Load Balancer<br/>F5 / HAProxy]
        
        subgraph "Web Tier"
            IIS1[Windows Server 2025<br/>IIS + .NET 10 Runtime]
            IIS2[Windows Server 2025<br/>IIS + .NET 10 Runtime]
        end
        
        subgraph "Database Tier"
            SQL1[(SQL Server 2022<br/>Primary)]
            SQL2[(SQL Server 2022<br/>Secondary / AG)]
        end
        
        subgraph "Monitoring"
            PROM[Prometheus + Grafana<br/>or SCOM]
        end
    end
    
    LB --> IIS1 & IIS2
    IIS1 & IIS2 --> SQL1
    SQL1 -.->|Always On AG| SQL2
    IIS1 & IIS2 -.-> PROM
```

| Attribute | Details |
|---|---|
| **Best For** | Organizations with existing Windows Server infrastructure, compliance requirements for data locality, or limited cloud readiness |
| **Deployment** | `dotnet publish -c Release -o ./publish` → Copy to IIS site → Configure app pool for .NET 10 |
| **CI/CD** | GitHub Actions / Azure DevOps → Build → Test → Deploy to IIS via WinRM or FTP |
| **Database** | SQL Server 2022 on-prem, Always On Availability Groups for HA |
| **Monitoring** | OpenTelemetry → Prometheus + Grafana, or SCOM |
| **Estimated Cost** | $15,000-25,000/yr (hardware amortization + licensing) |
| **Pros** | Full control, no cloud dependency, data stays local |
| **Cons** | Manual scaling, patch management burden, higher ops overhead |
| **Migration Effort** | Low (1-2 sprints) — straightforward IIS deployment |

---

### Option B: Azure App Service (PaaS)

```mermaid
graph TB
    subgraph "Azure Cloud"
        subgraph "Networking"
            AFD[Azure Front Door<br/>CDN + WAF]
        end
        
        subgraph "Compute"
            AS[Azure App Service<br/>.NET 10 Linux Plan<br/>P1v3]
            SLOT[Staging Slot<br/>Blue/Green Deploy]
        end
        
        subgraph "Data"
            ASQL[Azure SQL Database<br/>General Purpose S2]
        end
        
        subgraph "Identity & Security"
            MI[Managed Identity<br/>Passwordless Auth]
            KV[Azure Key Vault<br/>Connection Strings]
        end
        
        subgraph "Monitoring"
            AM[Azure Monitor<br/>OpenTelemetry]
            LA[Log Analytics<br/>Workspace]
        end
    end
    
    AFD --> AS
    AS -.-> SLOT
    AS --> ASQL
    AS -->|Managed Identity| MI
    MI --> KV & ASQL
    AS -.-> AM --> LA
    
    style MI fill:#2ecc71,color:#fff
    style KV fill:#2ecc71,color:#fff
```

| Attribute | Details |
|---|---|
| **Best For** | Teams wanting managed infrastructure, built-in scaling, and Azure ecosystem integration |
| **Deployment** | `az webapp deploy` or GitHub Actions → Azure App Service |
| **CI/CD** | GitHub Actions with `azure/webapps-deploy@v3` action, deployment slots for zero-downtime |
| **Database** | Azure SQL Database (General Purpose tier) with Managed Identity auth (passwordless) |
| **Monitoring** | Azure Monitor + OpenTelemetry (already configured in modernized app) |
| **Estimated Cost** | $300-600/mo (App Service P1v3 + Azure SQL S2 + monitoring) |
| **Pros** | Managed patching, auto-scale, deployment slots, built-in auth |
| **Cons** | Vendor lock-in, less control over infrastructure |
| **Migration Effort** | Low-Medium (2-3 sprints) — add Managed Identity, Key Vault, deploy |

**Key Azure Services:**
- Azure App Service (Linux, .NET 10)
- Azure SQL Database (serverless or provisioned)
- Azure Key Vault (secrets management)
- Azure Front Door (global load balancing + WAF)
- Azure Monitor (OpenTelemetry integration)
- Managed Identity (passwordless DB auth)

---

### Option C: Azure Container Apps (Containerized)

```mermaid
graph TB
    subgraph "Azure Cloud"
        subgraph "Container Platform"
            ACA[Azure Container Apps<br/>Environment]
            APP[eShop Container<br/>.NET 10 Linux]
            REV[Revision Management<br/>Traffic Splitting]
        end
        
        subgraph "Container Registry"
            ACR[Azure Container<br/>Registry]
        end
        
        subgraph "Data & Services"
            ASQL2[Azure SQL Database]
            KV2[Azure Key Vault]
            SB[Azure Service Bus ✨<br/>Event-driven future]
        end
        
        subgraph "Observability"
            AM2[Azure Monitor]
            DAPR[Dapr Sidecar ✨<br/>Optional]
        end
    end
    
    ACR -->|Image Pull| ACA
    ACA --> APP
    APP --> REV
    APP --> ASQL2
    APP --> KV2
    APP -.-> SB
    APP -.-> DAPR
    APP -.-> AM2
    
    style DAPR fill:#3498db,color:#fff
    style SB fill:#3498db,color:#fff
```

| Attribute | Details |
|---|---|
| **Best For** | Teams planning future microservice decomposition, needing container portability |
| **Deployment** | `docker build` → Azure Container Registry → Azure Container Apps revision |
| **CI/CD** | GitHub Actions → Build image → Push ACR → Deploy revision with traffic splitting |
| **Database** | Azure SQL Database with Managed Identity |
| **Monitoring** | Azure Monitor + OpenTelemetry + optional Dapr observability |
| **Estimated Cost** | $200-500/mo (Container Apps consumption + Azure SQL + ACR) |
| **Pros** | Container portability, Dapr integration, scale-to-zero, revision management |
| **Cons** | Requires Dockerfile knowledge, cold start latency on scale-to-zero |
| **Migration Effort** | Medium (3-4 sprints) — containerize, set up ACR, configure ACA |

**Dockerfile (for modernized app):**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/eShopModernized/eShopModernized.csproj", "src/eShopModernized/"]
RUN dotnet restore "src/eShopModernized/eShopModernized.csproj"
COPY . .
RUN dotnet publish "src/eShopModernized/eShopModernized.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "eShopModernized.dll"]
```

---

### Option D: Microservices Architecture

```mermaid
graph TB
    subgraph "API Gateway Layer"
        APIM[Azure API Management<br/>or YARP Reverse Proxy]
    end
    
    subgraph "Microservices"
        subgraph "Catalog Service"
            CS[Catalog API<br/>.NET 10]
            CDB[(Catalog DB)]
        end
        
        subgraph "Inventory Service"
            IS[Inventory API<br/>.NET 10]
            IDB[(Inventory DB)]
        end
        
        subgraph "Image Service"
            IMG[Image API<br/>.NET 10]
            BLOB[Azure Blob Storage]
        end
        
        subgraph "Reporting Service"
            RPT[Reporting API<br/>.NET 10]
            RPT -.->|Read Replica| CDB
        end
    end
    
    subgraph "Infrastructure"
        SB2[Azure Service Bus<br/>Event Messaging]
        ASPIRE[.NET Aspire<br/>Service Defaults ✨]
        OTEL[OpenTelemetry<br/>Distributed Tracing]
    end
    
    APIM --> CS & IS & IMG & RPT
    CS --> CDB
    IS --> IDB
    IMG --> BLOB
    CS <-->|Events| SB2
    IS <-->|Events| SB2
    CS & IS & IMG & RPT -.-> ASPIRE
    ASPIRE -.-> OTEL
    
    style ASPIRE fill:#9b59b6,color:#fff
    style SB2 fill:#3498db,color:#fff
```

| Attribute | Details |
|---|---|
| **Best For** | Large teams, high-scale requirements, independent deployment needs |
| **Service Decomposition** | 4 services: Catalog, Inventory, Image, Reporting |
| **Communication** | Sync: REST/gRPC via API Gateway; Async: Azure Service Bus events |
| **Data Strategy** | Database-per-service pattern (separate schemas or databases) |
| **Monitoring** | .NET Aspire + OpenTelemetry distributed tracing across services |
| **Estimated Cost** | $800-2000/mo (4 container apps + 2 DBs + Service Bus + APIM) |
| **Pros** | Independent scaling, team autonomy, technology flexibility |
| **Cons** | Distributed system complexity, eventual consistency, operational overhead |
| **Migration Effort** | High (6-10 sprints) — decompose monolith, implement messaging, separate data |

**Service Boundaries (Domain-Driven Design):**

| Service | Bounded Context | Data Owned | APIs |
|---|---|---|---|
| **Catalog** | Product information | CatalogItem, CatalogBrand, CatalogType | CRUD for products, search, browse |
| **Inventory** | Stock management | AvailableStock, RestockThreshold, OnReorder | Stock adjustments, reorder alerts |
| **Image** | Product media | PictureFileName mapping to blob storage | Upload, retrieve, resize images |
| **Reporting** | Business analytics | Read-only view of catalog + inventory | Inventory reports, sales analytics |

---

## Decision Matrix

```mermaid
quadrantChart
    title Deployment Strategy Decision Matrix
    x-axis Low Operational Complexity --> High Operational Complexity
    y-axis Low Scalability --> High Scalability
    quadrant-1 Best for Growth
    quadrant-2 Best for Enterprise
    quadrant-3 Best for Quick Start
    quadrant-4 Avoid Unless Required
    Option A On-Prem IIS: [0.25, 0.3]
    Option B App Service: [0.35, 0.6]
    Option C Container Apps: [0.55, 0.75]
    Option D Microservices: [0.85, 0.9]
```

| Criteria | A: On-Prem | B: App Service | C: Containers | D: Microservices |
|---|---|---|---|---|
| **Time to Deploy** | 1-2 sprints | 2-3 sprints | 3-4 sprints | 6-10 sprints |
| **Monthly Cost** | $1,250/mo* | $300-600/mo | $200-500/mo | $800-2,000/mo |
| **Scalability** | Manual | Auto (1-30 instances) | Auto (0-N, scale to zero) | Per-service auto-scale |
| **Ops Burden** | High (OS patching, certs) | Low (managed) | Medium (containers) | High (distributed system) |
| **Team Size Needed** | 1-2 ops + 2-3 dev | 2-3 dev | 3-4 dev + 1 DevOps | 4-6 dev + 2 DevOps |
| **Lock-in Risk** | None | Medium (Azure) | Low (containers portable) | Medium (Azure services) |
| **Future-Proof** | ⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ |

*\* On-prem cost = hardware amortization ($15-25k/yr ÷ 12)*

---

## Recommended Migration Roadmap

### Phased Approach (Recommended)

```mermaid
gantt
    title Migration Roadmap — Phased Approach
    dateFormat  YYYY-MM-DD
    section Phase 1: Lift & Shift
        Deploy to App Service (Option B)     :a1, 2026-03-10, 3w
        Configure Azure SQL + Managed Identity :a2, 2026-03-10, 2w
        Set up CI/CD pipeline                  :a3, after a2, 1w
        Production validation                  :a4, after a1, 1w
    
    section Phase 2: Containerize
        Create Dockerfile                      :b1, after a4, 1w
        Set up Azure Container Registry        :b2, after a4, 1w
        Migrate to Container Apps (Option C)   :b3, after b1, 2w
        Configure auto-scaling                 :b4, after b3, 1w
    
    section Phase 3: Decompose (Optional)
        Extract Inventory Service              :c1, after b4, 3w
        Extract Image Service                  :c2, after c1, 2w
        Implement Service Bus messaging        :c3, after c1, 2w
        Add .NET Aspire orchestration          :c4, after c2, 2w
        Extract Reporting Service              :c5, after c3, 2w
    
    section Phase 4: Optimize
        Performance testing                    :d1, after c5, 2w
        Cost optimization                      :d2, after d1, 1w
        Production cutover                     :d3, after d2, 1w
```

### Sprint-by-Sprint Plan

| Sprint | Duration | Activities | Deliverable |
|---|---|---|---|
| **Sprint 1** | 2 weeks | Deploy modernized monolith to Azure App Service, configure Azure SQL, set up Managed Identity | Working app on Azure |
| **Sprint 2** | 2 weeks | CI/CD pipeline (GitHub Actions), deployment slots, blue/green deploy | Automated deployment |
| **Sprint 3** | 2 weeks | Observability: Azure Monitor + OpenTelemetry dashboards, alerts | Full monitoring |
| **Sprint 4** | 2 weeks | Containerization: Dockerfile, ACR, migrate to Container Apps | Container-based deployment |
| **Sprint 5** | 2 weeks | Auto-scaling rules, performance baseline testing | Scalable infrastructure |
| **Sprint 6** | 2 weeks | (Optional) Extract Inventory as separate microservice | First microservice |
| **Sprint 7** | 2 weeks | (Optional) Service Bus integration, event-driven inventory updates | Async messaging |
| **Sprint 8** | 2 weeks | (Optional) Extract Image service → Azure Blob Storage | Media microservice |

---

## Cost Estimation Framework

### Azure Monthly Cost (Option B → C progression)

```
Phase 1: App Service Deployment
├── App Service P1v3 (Linux)        $138/mo
├── Azure SQL General Purpose (2 vCores) $200/mo
├── Azure Key Vault                 $0.03/10,000 ops
├── Azure Monitor (5GB logs)        $12/mo
├── Azure Front Door (Standard)     $35/mo
└── TOTAL                           ~$385/mo

Phase 2: Container Apps Deployment  
├── Container Apps (consumption)    $50-150/mo (scale to zero)
├── Azure Container Registry        $5/mo (Basic)
├── Azure SQL (unchanged)           $200/mo
├── Azure Monitor (unchanged)       $12/mo
└── TOTAL                           ~$267-367/mo

Phase 3: Microservices (4 services)
├── Container Apps (4 services)     $150-400/mo
├── Azure SQL (2 databases)         $400/mo
├── Azure Blob Storage              $5/mo
├── Azure Service Bus (Standard)    $10/mo
├── API Management (Developer)      $50/mo
├── Azure Monitor (10GB logs)       $24/mo
└── TOTAL                           ~$639-889/mo
```

---

## .NET Aspire Integration (For Options C & D)

If the customer chooses containerized or microservice deployment, add .NET Aspire for:

```csharp
// AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql")
    .AddDatabase("catalogdb");

var catalog = builder.AddProject<Projects.eShopModernized>("catalog")
    .WithReference(sql)
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

**Benefits:**
- Service discovery and configuration
- Health check aggregation
- OpenTelemetry auto-configuration
- Local development orchestration
- Dashboard for all services

---

## PM Recommendations

1. **Start with Option B (App Service)** — lowest risk, fastest time to production
2. **Evolve to Option C (Container Apps)** when scaling requirements emerge
3. **Consider Option D (Microservices)** only if team grows to 8+ and independent deployment is required
4. **Avoid Option A (On-Prem)** unless regulatory/compliance mandates data locality

**Key Decision Points:**
- If < 3 dev team → Stay with Option B
- If need scale-to-zero → Move to Option C
- If 4+ teams need independent deploys → Consider Option D
- If data sovereignty required → Option A or Azure Government

---

*Generated by PM Migration Agent — Deployment planning phase*
*Uses HVE Core rpi-agent methodology*
*Planning Date: March 5, 2026*
