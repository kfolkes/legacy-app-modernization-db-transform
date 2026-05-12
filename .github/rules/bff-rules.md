# Backend-for-Frontend (BFF) Pattern Rules

> Reference: [Microsoft Architecture Center — Backends for Frontends](https://learn.microsoft.com/en-us/azure/architecture/patterns/backends-for-frontends)

## Core Principles

1. **One BFF per frontend experience** — Mobile BFF, Web BFF, Internal Tools BFF are SEPARATE services. Never share a single BFF across fundamentally different client types.
2. **BFF ≠ API Gateway** — An API Gateway handles cross-cutting concerns (rate limiting, auth termination, routing). A BFF handles **experience-specific aggregation, transformation, and orchestration** for a specific frontend.
3. **BFF owns the frontend contract** — The BFF defines the response shape optimized for its frontend. Domain services return canonical models; the BFF transforms them.

## Mandatory Patterns

### Aggregation & Composition
- BFF aggregates responses from multiple downstream microservices into a single response optimized for the frontend.
- Use parallel `Task.WhenAll` (C#) or `async/awaitAll` (Kotlin coroutines) or `Promise.all` (TypeScript) for concurrent downstream calls.
- Never expose raw microservice models to the frontend — always transform to frontend-optimized DTOs.

### Authentication & Token Exchange
- Frontend authenticates with Entra ID (MSAL.js, MSAL4J, or MSAL.NET).
- BFF performs **on-behalf-of (OBO) token exchange** to call downstream services with delegated identity.
- Never pass frontend tokens directly to domain services — always exchange through the BFF.
- Store tokens server-side (encrypted session or token cache) — never in client-side storage for web BFFs.

### Resilience
- Implement **circuit breaker** for every downstream service call (Polly for .NET, Resilience4j for Kotlin, built-in fetch retry for Next.js).
- Define **fallback responses** for degraded mode (cached data, skeleton responses).
- Set **timeouts** on all downstream calls (default: 5s for reads, 15s for writes).
- Use **bulkhead isolation** to prevent one slow downstream from blocking all requests.

### Caching
- BFF may cache downstream responses for short TTLs (30s–5min) to reduce load.
- Cache keys must include user identity context (no cross-user cache pollution).
- Use Redis or in-memory cache with explicit eviction policies.

### Health & Observability
- Expose `/health` (liveness) and `/ready` (includes downstream dependency checks) endpoints.
- Propagate distributed tracing headers (`traceparent`, `tracestate`) from frontend through BFF to all downstream calls.
- Use OpenTelemetry for traces, metrics, and logs — integrated with Azure Monitor.
- Log all downstream call durations and status codes at INFO level.

## Anti-Patterns (NEVER do these)

- **God BFF**: A single BFF serving all frontends — defeats the purpose.
- **Business logic in BFF**: BFF orchestrates and transforms — domain logic belongs in domain services.
- **Direct database access from BFF**: BFF calls services, NEVER databases.
- **Synchronous chain of BFF→Service→Service**: If the BFF needs data from Service B via Service A, refactor to have BFF call both directly.
- **BFF as a caching layer for everything**: BFF caches for UX performance, NOT as a system-wide cache.

## Communication Patterns

| Direction | Protocol | When |
|---|---|---|
| Frontend → BFF | HTTPS REST (JSON) | Always for web and mobile |
| BFF → Domain Services | gRPC (preferred) | High-performance internal calls |
| BFF → Domain Services | REST (fallback) | When gRPC not available |
| BFF → Event Bus | Azure Service Bus / Event Hubs | Fire-and-forget commands or event publishing |

## Security Requirements

- All BFF endpoints require authentication (no anonymous access except `/health`).
- Apply CORS policies: whitelist only the specific frontend origin.
- Rate limit per-user to prevent abuse.
- Validate and sanitize all input from the frontend before forwarding to domain services.
- Use HTTPS/TLS 1.3 for all communication (frontend↔BFF and BFF↔services).
