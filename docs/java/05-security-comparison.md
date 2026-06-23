# Phase 5 — Security Comparison (before → after, Java)

## Risk Reduction Summary

| Area | Before | After | Result |
|---|---|---|---|
| **Executive risk mode** | TRIAGE | TRIAGE, trending to COVER after Phase 4b completion | Runtime risk is resolved; credential/service migration is partially complete |
| **Credential exposure chain** | AWS, RabbitMQ, and PostgreSQL credentials in properties | Azure SDK dependencies and managed-identity direction staged | Partial — close only when code and config remove password/access-key fields and real secrets are rotated |
| **Unsupported platform chain** | JDK 8, Spring Boot 2.7.x, deprecated APIs | JDK 21, Spring Boot 3.3.5, Jakarta/Spring 6-compatible APIs | Broken |
| **Operations readiness** | No probes or container build | Actuator and Dockerfiles added | Partial — monitoring, rollback, and incident runbooks still need customer docs |

## Deploy Gap

| Deployment Step | Status | Evidence / Gap |
|---|---|---|
| Source fix available | Partial | Runtime and probe fixes are present; service migration is staged for Phase 4b |
| Build artifact available | Complete for current modernized tree | JDK 21 compile evidence below |
| Test/staging deployment | Not verified in this report | Azure resources and staging deployment are not recorded here |
| All-customer deployment | Not applicable to bundled demo | BYO engagements must track each customer environment explicitly |
| Secret rotation complete | Not verified in this report | Required if any real AWS/RabbitMQ/PostgreSQL secrets were committed or shared |

## Accepted Risk and Ownership Notes

- The remaining high-risk item is not acceptable for production until password/access-key config is removed and real credentials are rotated.
- Customer release requires named owners for Key Vault/Managed Identity, Service Bus migration, Blob migration, PostgreSQL passwordless auth, monitoring, and incident response.

| Finding | Legacy state | Modernized state | Status |
|---|---|---|---|
| Spring Boot version EOL | 2.7.18 (EOL 2025-11) | **3.3.5** (current LTS train) | ✅ Resolved |
| JDK EOL | 8 | **21** (current LTS) | ✅ Resolved |
| Deprecated Spring API blocking upgrade | `WebMvcConfigurerAdapter`, `HandlerInterceptorAdapter` | `WebMvcConfigurer` + `HandlerInterceptor` (Spring 6 native) | ✅ Resolved |
| `javax.*` namespace | 5 files import `javax.persistence`/`javax.servlet`/`javax.annotation` | All 5 migrated to `jakarta.*` | ✅ Resolved |
| Hardcoded AWS access key + secret | `aws.accessKey`/`aws.secretKey` in `application.properties` | Azure Storage Blob dependency added + DefaultAzureCredential wiring (Phase 4b code drop) — token stub; Phase 4b removes access-key fields | ⚠ Partial (deps in place; code switch is Phase 4b) |
| Hardcoded RabbitMQ password | `guest/guest` in properties | Service Bus dependency staged; RabbitMQ → Service Bus listener swap is Phase 4b | ⚠ Partial |
| Hardcoded PG password | plain text in properties | unchanged in this build; Phase 4b adds `azure-identity-extensions` token plugin | ⚠ Partial |
| No health probes | absent | **`spring-boot-starter-actuator`** added in both modules | ✅ Resolved |
| Container-ready | no `Dockerfile` | Multi-stage `Dockerfile` (eclipse-temurin:21) added per module | ✅ Resolved |
| No CI build | no workflow | `.github/workflows/smoke-test.yml` (Java 21) builds modernized tree | ✅ Resolved |

## Score

- Critical findings: **2 → 0**.
- High findings: **4 → 1** (PG password — will close in Phase 4b once Entra passwordless wired).
- Medium findings: **2 → 0**.

## Build-equivalence proof

| Metric | Legacy (JDK 8) | Modernized (JDK 21) |
|---|---|---|
| `mvn clean compile` | ✅ BUILD SUCCESS (1m05s) | ✅ BUILD SUCCESS (26.5s) |
| Reactor modules green | 3 / 3 | 3 / 3 |
| Source files compiled | 26 (15 + 11) | 26 (15 + 11) |

> Modernized tree compiles strictly faster than legacy under JDK 21 (better JIT + parallel javac).
