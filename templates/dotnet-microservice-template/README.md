# .NET 10 Microservice Boilerplate Template

> Part of the eShop modernization demo — see [microservice-rules.md](../../.github/rules/microservice-rules.md)

## What This Template Provides

A ready-to-use .NET 10 microservice with:

- **SDK-style csproj** targeting `net10.0`
- **Minimal hosting** `Program.cs` with full middleware pipeline
- **Dual database provider**: SQL Server + PostgreSQL (switchable via config)
- **EF Core 10** with HiLo sequences
- **Authentication**: Entra ID via `Microsoft.Identity.Web`
- **Authorization**: ASP.NET Core policies + OPA sidecar integration
- **Messaging**: MassTransit + Azure Service Bus + Azure Event Hubs
- **Observability**: OpenTelemetry → Azure Monitor, Serilog structured logging
- **Health checks**: `/health` (liveness) + `/ready` (readiness)
- **gRPC**: Server and client support
- **Docker**: Multi-stage build, non-root user, health check
- **Security headers**: HSTS, X-Content-Type-Options, X-Frame-Options

## Usage

1. Copy this template folder
2. Rename `ServiceName` → your service name (e.g., `CatalogService`)
3. Update `appsettings.json` with your connection strings and Entra ID config
4. Add your domain models, controllers, and EF Core DbContext
5. Register in the Aspire AppHost (see `templates/aspire-orchestrator-template/`)

## Quick Start

```bash
cd templates/dotnet-microservice-template/src/ServiceName
dotnet restore
dotnet run
```

## Architecture Alignment

| Pattern | Implementation | Reference |
|---|---|---|
| Microservices | Database-per-service, bounded context | [Architecture Center](https://learn.microsoft.com/en-us/azure/architecture/reference-architectures/containers/aks-microservices/aks-microservices) |
| Event-driven | MassTransit + Service Bus + Event Hubs | [Event-driven architecture](https://learn.microsoft.com/en-us/azure/architecture/guide/architecture-styles/event-driven) |
| RBAC + OPA | ASP.NET Core policies + OPA sidecar | [Zero trust](https://learn.microsoft.com/en-us/azure/architecture/guide/security/conditional-access-architecture) |
| Observability | OpenTelemetry → Azure Monitor | [Monitoring microservices](https://learn.microsoft.com/en-us/azure/architecture/microservices/logging-monitoring) |
