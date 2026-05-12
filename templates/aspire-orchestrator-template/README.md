# .NET Aspire Orchestrator Template

> Part of the eShop modernization demo — see [microservice-rules.md](../../.github/rules/microservice-rules.md)

## What This Template Provides

The **Aspire AppHost** that orchestrates the entire eShop modernized architecture:

### Infrastructure Resources
- **PostgreSQL** with PgAdmin — Catalog DB + Inventory DB
- **Redis** with Redis Commander — distributed caching for BFFs
- **Azure Service Bus** — command queues + domain event topics
- **Azure Event Hubs** — streaming (price changes, stock alerts, audit events)
- **Azure Application Insights** — centralized observability

### Services
- **Catalog Service** (.NET 10 microservice)
- **Inventory Service** (.NET 10 microservice)
- **Image Service** (.NET 10 microservice)
- **Reporting Service** (.NET 10 microservice)

### BFF Layer
- **Kotlin BFF** (Ktor — Docker container, port 8080)
- **React BFF** (Next.js 15 — npm app, port 3000)

### Authorization
- **OPA Sidecar** (Docker container, port 8181)

## Architecture Diagram

```
                     ┌─────────────────────────────────────────┐
                     │          .NET Aspire AppHost             │
                     └─────────────────────────────────────────┘
                              |              |
                    ┌─────────┴──────┐  ┌────┴─────────┐
                    │  Kotlin BFF    │  │  React BFF    │
                    │  (Ktor:8080)   │  │  (Next:3000)  │
                    └─────┬──────────┘  └──────┬────────┘
                          |                     |
              ┌───────────┼─────────────────────┼──────────────┐
              |           |                     |              |
         ┌────┴───┐ ┌────┴───┐ ┌──────────┐ ┌──┴──────────┐
         │Catalog │ │Invntry │ │  Image   │ │  Reporting  │
         │Service │ │Service │ │ Service  │ │  Service    │
         └───┬────┘ └───┬────┘ └──────────┘ └─────────────┘
             |          |
        ┌────┴───┐ ┌────┴───┐    ┌───────┐    ┌──────────┐
        │CatalogDB│ │InvntryDB│  │ Redis │    │   OPA    │
        │(PgSQL) │ │(PgSQL) │   │(Cache)│    │(Policy)  │
        └────────┘ └────────┘   └───────┘    └──────────┘
                        |
                ┌───────┴───────┐
                │  Service Bus  │  Event Hubs
                │  (Commands)   │  (Streaming)
                └───────────────┘
```

## Quick Start

```bash
cd templates/aspire-orchestrator-template/src/eShop.AppHost
dotnet run
# Opens the Aspire Dashboard at https://localhost:15888
```

## Reference

- [.NET Aspire Overview](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview)
- [Aspire with existing apps](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/add-aspire-existing-app)
- [Service Discovery](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/service-discovery)
