# Kotlin BFF Boilerplate Template (Ktor 3.x)

> Part of the eShop modernization demo — see [kotlin-bff-rules.md](../../.github/rules/kotlin-bff-rules.md)

## What This Template Provides

A ready-to-use Kotlin BFF service with:

- **Ktor 3.x** server with Netty engine
- **Kotlin 2.0** + JDK 21
- **BFF aggregation pattern**: Combines Catalog + Inventory data for frontend
- **Coroutine-based** async downstream calls
- **Resilience4j** circuit breakers per downstream service
- **JWT authentication** (Azure Entra ID)
- **OpenTelemetry** metrics via Micrometer OTLP
- **Structured logging**: Logback + Logstash encoder (JSON)
- **CORS** configuration
- **Health endpoints**: `/health` and `/ready`
- **Docker**: Multi-stage build, non-root user, JRE-only runtime

## Project Structure

```
src/main/kotlin/com/eshop/bff/
├── Application.kt              # Entry point + module configuration
├── plugins/
│   ├── Serialization.kt        # kotlinx.serialization JSON config
│   ├── Security.kt             # JWT/Entra ID auth
│   ├── Monitoring.kt           # Micrometer + call logging
│   ├── HTTP.kt                 # CORS
│   └── StatusPages.kt          # Error handling
├── routes/
│   ├── CatalogRoutes.kt        # BFF aggregation routes
│   ├── InventoryRoutes.kt      # Inventory passthrough
│   └── HealthRoutes.kt         # Health + readiness
├── clients/
│   ├── CatalogServiceClient.kt # Downstream client with circuit breaker
│   └── InventoryServiceClient.kt
└── models/
    └── Models.kt               # Shared data classes
```

## Quick Start

```bash
cd templates/kotlin-bff-template
./gradlew run
# Server starts at http://localhost:8080
```

## Architecture Alignment

| Pattern | Implementation | Reference |
|---|---|---|
| BFF | 1 BFF per frontend, aggregation routes | [BFF Pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/backends-for-frontends) |
| Circuit Breaker | Resilience4j per downstream service | [Circuit Breaker](https://learn.microsoft.com/en-us/azure/architecture/patterns/circuit-breaker) |
| Gateway Routing | Ktor routes to downstream microservices | [Gateway Routing](https://learn.microsoft.com/en-us/azure/architecture/patterns/gateway-routing) |
