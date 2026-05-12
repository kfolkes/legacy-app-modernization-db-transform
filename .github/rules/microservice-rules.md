# Microservice Architecture Rules

> References:
> - [Microsoft Architecture Center — Microservices on AKS](https://learn.microsoft.com/en-us/azure/architecture/reference-architectures/containers/aks-microservices/aks-microservices)
> - [Event-driven architecture](https://learn.microsoft.com/en-us/azure/architecture/guide/architecture-styles/event-driven)
> - [Decompose a monolith](https://learn.microsoft.com/en-us/azure/architecture/microservices/migrate-monolith)
> - [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview)

## Service Decomposition (Domain-Driven Design)

### Bounded Contexts for eShop

| Service | Domain | Owns Data | Key Entities |
|---|---|---|---|
| **Catalog Service** | Product catalog management | `catalogdb` | CatalogItem, CatalogBrand, CatalogType |
| **Inventory Service** | Stock management + business rules | `inventorydb` | StockLevel, ReorderPolicy, AdjustmentHistory |
| **Image Service** | Media asset management | Azure Blob Storage | ImageMetadata |
| **Reporting Service** | Analytics and dashboards | Read replica of catalogdb | (read-only views) |

### Rules
1. **Database-per-service** — each service owns its data store. No shared databases.
2. **Service boundary = team boundary** — a service should be owned by ONE team.
3. **Loose coupling** — services communicate via APIs and events, NEVER by sharing code or libraries (except shared `.proto` files for gRPC contracts).
4. **High cohesion** — related business logic lives in the same service.
5. **Independently deployable** — each service has its own CI/CD pipeline, Docker image, and deployment configuration.

## Communication Patterns

### Synchronous (Request/Reply)

| Protocol | Use When | Technology |
|---|---|---|
| **gRPC** (preferred) | Service-to-service internal calls, high throughput | `Grpc.AspNetCore` (.NET), `grpc-kotlin` (Kotlin) |
| **REST** (fallback) | External APIs, BFF→Service when gRPC not viable | ASP.NET Core Minimal APIs |

### Asynchronous (Event-Driven)

| Pattern | Technology | Use When |
|---|---|---|
| **Domain Events** (streaming) | **Azure Event Hubs** | High-throughput event streams: stock changes, page views, telemetry |
| **Commands** (reliable messaging) | **Azure Service Bus** | Guaranteed delivery: OrderPlaced, StockAdjusted, PriceUpdated |
| **Integration Events** | Service Bus Topics | Cross-service notifications that multiple subscribers need |

### Event-Driven Wiring

```
┌─────────────┐     StockAdjusted      ┌─────────────────┐
│  Inventory   │ ──── Event Hubs ─────► │   Reporting      │
│  Service     │                        │   Service        │
└─────────────┘                        └─────────────────┘
       │                                        
       │  StockCritical                         
       └──── Service Bus Topic ────►  [Notification Service]
                                      [Reorder Service]
```

### Event Schema (CloudEvents v1.0)
```json
{
  "specversion": "1.0",
  "type": "com.eshop.inventory.stock-adjusted",
  "source": "/services/inventory",
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "time": "2026-03-10T12:00:00Z",
  "datacontenttype": "application/json",
  "data": {
    "catalogItemId": 42,
    "previousStock": 100,
    "newStock": 85,
    "adjustedBy": "user@contoso.com"
  }
}
```
- All events use **CloudEvents v1.0** specification.
- Events are **immutable facts** — they record what happened, not commands.
- Include `correlationId` from the originating HTTP request for distributed tracing.

## .NET Aspire Orchestration

### AppHost Configuration
```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure
var postgres = builder.AddPostgres("postgres")
    .AddDatabase("catalogdb")
    .AddDatabase("inventorydb");
var redis = builder.AddRedis("cache");
var serviceBus = builder.AddAzureServiceBus("messaging");
var eventHubs = builder.AddAzureEventHubs("streaming");

// Services
var catalogService = builder.AddProject<Projects.CatalogService>("catalog")
    .WithReference(postgres)
    .WithReference(redis)
    .WithExternalHttpEndpoints();

var inventoryService = builder.AddProject<Projects.InventoryService>("inventory")
    .WithReference(postgres)
    .WithReference(serviceBus)
    .WithReference(eventHubs)
    .WithExternalHttpEndpoints();

// BFFs
builder.AddProject<Projects.KotlinBff>("kotlin-bff")
    .WithReference(catalogService)
    .WithReference(inventoryService)
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

### Rules
1. **Every microservice MUST be registered** in the Aspire AppHost.
2. **Service discovery** via Aspire — services reference each other by name, not URLs.
3. **Health checks** aggregated in Aspire Dashboard — use `AddHealthCheck()` on each service.
4. **OpenTelemetry** auto-configured by Aspire — traces, metrics, logs all flow to the Dashboard.
5. **Local development**: `dotnet run --project AppHost` starts ALL services + infrastructure.

## RBAC + Authorization

### Infrastructure RBAC (Azure)
- Azure RBAC for resource access (Key Vault, Storage, Service Bus).
- Managed Identity for all service-to-Azure-resource authentication. NEVER use connection strings with passwords.

### Application RBAC (ASP.NET Core)
```csharp
// Program.cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CatalogAdmin", policy =>
        policy.RequireRole("CatalogAdmin")
              .RequireClaim("department"));
    options.AddPolicy("InventoryManager", policy =>
        policy.RequireRole("InventoryManager"));
    options.AddPolicy("ReadOnly", policy =>
        policy.RequireAuthenticatedUser());
});
```

### Fine-Grained Authorization (OPA)
See [OPA + RBAC Rules](opa-rbac-rules.md) for detailed OPA integration.

## Observability (OpenTelemetry)

### Every service MUST implement:
1. **Distributed tracing** — propagate `traceparent` through ALL calls (HTTP, gRPC, Service Bus, Event Hubs).
2. **Custom metrics** — `service.request.duration`, `service.error.count`, `service.event.published`.
3. **Structured logging** — Serilog with JSON output, `TraceId` and `SpanId` in every log entry.
4. **Health checks** — `/health` (liveness), `/ready` (readiness with dependency checks).

### Azure Monitor Integration
```csharp
builder.Services.AddOpenTelemetry()
    .UseAzureMonitor(options =>
    {
        options.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
    })
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddGrpcClientInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("eShop.*"))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation());
```

## Docker & Deployment

### Every service MUST have:
1. **Multi-stage Dockerfile** — build stage + runtime stage (Alpine-based for minimal image size).
2. **Non-root user** — `USER app` in Dockerfile.
3. **Health check instruction** — `HEALTHCHECK CMD curl -f http://localhost:8080/health || exit 1`.
4. **Label metadata** — `LABEL maintainer`, `version`, `description`.

### Container Apps Deployment
- Azure Container Apps for serverless scaling (scale to zero when idle).
- Dapr sidecar optional but recommended for pub/sub abstraction.
- Revision-based deployments with traffic splitting (blue/green).

## Anti-Patterns (NEVER do these)

- **Distributed monolith**: Services that must all be deployed together or that share databases.
- **Synchronous chains**: Service A → Service B → Service C synchronously. Use events or parallel calls.
- **Shared libraries with business logic**: Shared proto files and DTOs are fine. Shared domain logic is NOT.
- **Manual service discovery**: Hardcoded URLs. Use Aspire service discovery or DNS.
- **Microservices for small teams**: If fewer than 6 developers, start with a modular monolith and decompose later.
