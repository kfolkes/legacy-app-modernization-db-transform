# Kotlin BFF Coding Standards

> Reference: [Microsoft Architecture Center — API Gateway pattern](https://learn.microsoft.com/en-us/azure/architecture/microservices/design/gateway)  
> Companion: [BFF Rules](bff-rules.md) — read that first for pattern-level rules.

## Framework Choice

| Option | Use When | Framework |
|---|---|---|
| **Ktor** (recommended for this demo) | Lightweight, coroutine-native, minimal ceremony | Ktor 3.x + Kotlin Coroutines |
| **Spring Boot WebFlux** | Enterprise tooling, reactive ecosystem, team familiarity | Spring Boot 3.x + WebFlux + Kotlin coroutines |

Both options MUST follow all rules below.

## Project Structure

```
kotlin-bff/
├── build.gradle.kts              # Kotlin DSL, net JVM 21 target
├── settings.gradle.kts
├── Dockerfile                    # Multi-stage build (gradle → JRE 21 slim)
├── src/main/kotlin/com/demo/bff/
│   ├── Application.kt            # Entry point (Ktor: embeddedServer / Spring: @SpringBootApplication)
│   ├── config/
│   │   ├── SecurityConfig.kt     # MSAL4J / Entra ID auth
│   │   ├── ServiceDiscovery.kt   # Downstream service URLs from env/config
│   │   └── ObservabilityConfig.kt # OpenTelemetry setup
│   ├── routes/                   # (Ktor) or controllers/ (Spring)
│   │   ├── CatalogRoutes.kt      # GET /api/catalog, GET /api/catalog/{id}
│   │   └── InventoryRoutes.kt    # GET /api/inventory, POST /api/inventory/{id}/adjust
│   ├── clients/
│   │   ├── CatalogServiceClient.kt   # gRPC/REST client to Catalog Service
│   │   └── InventoryServiceClient.kt # gRPC/REST client to Inventory Service
│   ├── aggregators/
│   │   └── CatalogAggregator.kt  # Combines Catalog + Inventory into frontend DTO
│   ├── dto/
│   │   ├── CatalogResponse.kt    # Frontend-optimized response model
│   │   └── InventoryResponse.kt
│   ├── resilience/
│   │   └── CircuitBreakerConfig.kt # Resilience4j configuration
│   └── health/
│       └── HealthRoutes.kt       # /health, /ready endpoints
├── src/main/resources/
│   ├── application.yaml           # Configuration
│   └── logback.xml                # Structured logging (JSON format)
└── src/test/kotlin/com/demo/bff/
    ├── CatalogRoutesTest.kt
    └── CatalogAggregatorTest.kt
```

## Mandatory Conventions

### Language & Build
- **Kotlin 2.x** on **JDK 21** (LTS).
- **Gradle Kotlin DSL** (`build.gradle.kts`) — never Groovy.
- Coroutines for ALL async operations — never blocking threads.
- `kotlinx-serialization` for JSON (Ktor) or Jackson Kotlin module (Spring).
- Explicit null safety — never use `!!` operator. Use `?:` with meaningful defaults or `requireNotNull` with messages.

### Authentication (Entra ID)
```kotlin
// MSAL4J confidential client — acquire token on-behalf-of
val msalClient = ConfidentialClientApplication.builder(clientId, credential)
    .authority("https://login.microsoftonline.com/$tenantId")
    .build()

suspend fun exchangeToken(upstreamToken: String): String {
    val oboParams = OnBehalfOfParameters.builder(scopes, UserAssertion(upstreamToken)).build()
    return msalClient.acquireToken(oboParams).get().accessToken()
}
```

### Downstream Communication
- **gRPC preferred** for internal service calls (generated from shared `.proto` files).
- **REST fallback** via Ktor HttpClient or WebClient (Spring).
- All clients MUST set timeout (`connectTimeout = 5.seconds`, `requestTimeout = 10.seconds`).
- Propagate `traceparent` header for distributed tracing.

### Resilience (Resilience4j)
```kotlin
val circuitBreaker = CircuitBreaker.of("catalogService", CircuitBreakerConfig.custom()
    .failureRateThreshold(50f)
    .waitDurationInOpenState(Duration.ofSeconds(30))
    .slidingWindowSize(10)
    .build())
```
- Circuit breaker on EVERY downstream client.
- Retry with exponential backoff: max 3 attempts, 100ms→200ms→400ms.
- Bulkhead: max 25 concurrent calls per downstream service.

### Observability (OpenTelemetry)
- Use `opentelemetry-kotlin` or `opentelemetry-javaagent` for auto-instrumentation.
- Export traces and metrics to Azure Monitor via OTLP exporter.
- Structured logging via SLF4J + Logback with JSON encoder.
- Log correlation: inject `traceId` and `spanId` into every log line.
- Custom metrics: `bff.downstream.latency`, `bff.aggregation.count`, `bff.auth.token_exchange.duration`.

### Health Endpoints
```kotlin
// /health — liveness (always 200 if process is running)
// /ready — readiness (checks all downstream services reachable)
get("/health") { call.respond(HttpStatusCode.OK, mapOf("status" to "healthy")) }
get("/ready") {
    val catalogReady = catalogClient.healthCheck()
    val inventoryReady = inventoryClient.healthCheck()
    val status = if (catalogReady && inventoryReady) HttpStatusCode.OK else HttpStatusCode.ServiceUnavailable
    call.respond(status, mapOf("catalog" to catalogReady, "inventory" to inventoryReady))
}
```

### Docker
```dockerfile
# Multi-stage build
FROM gradle:8-jdk21 AS build
COPY --chown=gradle:gradle . /home/gradle/src
WORKDIR /home/gradle/src
RUN gradle shadowJar --no-daemon

FROM eclipse-temurin:21-jre-alpine
COPY --from=build /home/gradle/src/build/libs/*.jar /app/app.jar
EXPOSE 8080
ENTRYPOINT ["java", "-jar", "/app/app.jar"]
```

## Anti-Patterns (NEVER do these)

- **Blocking coroutine scope**: Never use `runBlocking` inside request handlers.
- **Business logic in BFF**: Kotlin BFF aggregates and transforms — domain rules stay in .NET 10 services.
- **Direct DB access**: BFF NEVER connects to a database. All data comes from service calls.
- **Hardcoded URLs**: All service endpoints from configuration (environment variables or application.yaml).
- **Fat DTOs**: BFF DTOs are frontend-optimized — don't expose internal service models.

## Testing Standards

- Unit tests: JUnit 5 + Mockk for mocking downstream clients.
- Integration tests: Testcontainers for downstream service mocks.
- Minimum coverage: 80% on aggregators and route handlers.
- Test circuit breaker behavior: verify fallback responses when downstream is unavailable.
