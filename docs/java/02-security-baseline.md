# Phase 2 — Security Baseline (Java)

> Cross-validated by: dependency manifest review + Spring/Hibernate EOL data + source scan for hardcoded credentials.

## Executive Risk Mode

**Mode: TRIAGE.** This app has the classic modernization security chain: EOL runtime, EOL framework train, hardcoded cloud/service credentials, and no health/detection baseline for the future container platform. The issue is not just that the dependencies are old; it is that credential exposure, unsupported runtime, and missing operational evidence all land in the same migration path.

| Factor | Assessment |
|---|---|
| **Exposure** | Local/sample by default; customer deployments commonly expose the web module and connect to managed services |
| **Data sensitivity** | Asset metadata, storage objects, queue messages, and database credentials; customer variants may include regulated documents or vendor data |
| **Finding density** | 2 Critical and 4 High findings clustered around unsupported platform and secrets |
| **Worst realistic chain** | Application properties expose service credentials, attacker reaches object storage/message broker/database, and missing runbooks delay rotation and containment |

## Finding Chains

### Chain 1: Source-Controlled Credentials Become Service Access

- **Entry point:** `application.properties` in web and worker modules.
- **Weaknesses chained:** AWS access keys, RabbitMQ password, PostgreSQL password, no documented rotation owner.
- **Business impact:** asset data, queue processing, and database access can be misused after code is fixed unless real credentials are rotated.
- **Required fixes:** remove credentials from source, migrate to Managed Identity/DefaultAzureCredential, use Key Vault where needed, rotate any real exposed values, document owner.
- **Owner:** Not documented in baseline evidence.
- **Deploy gap:** Not documented; must be tracked through all customer environments.

### Chain 2: Unsupported Platform Blocks Security Response

- **Entry point:** JDK 8 and Spring Boot 2.7.x.
- **Weaknesses chained:** EOL runtime/framework, deprecated Spring APIs, no actuator probes, no container deploy evidence.
- **Business impact:** future CVEs and operational incidents become harder to patch, deploy, and observe.
- **Required fixes:** upgrade to Java 21 + Spring Boot 3.3, migrate `javax` to `jakarta`, add actuator probes, containerize, document rollback.

## Missing Controls

- **Detection:** No evidence of alerting, security event queries, or service-specific monitoring.
- **Incident response:** No documented call path for credential exposure, queue abuse, or storage compromise.
- **Rotation:** No documented process for cloud/service credential rotation.
- **Escalation owner:** No named owner for accepting or remediating baseline risk.
- **Customer deployment timeline:** No evidence for how long fixes take to reach every customer environment.

## Severity rollup (legacy state)

| Severity | Count | Notes |
|---|---|---|
| Critical | 2 | Spring Boot 2.7.18 (EOL 2025-11), JDK 8 (Oracle premier support ended; CVEs unpatched for most distros) |
| High | 4 | Password-based auth for S3, RabbitMQ, PostgreSQL; secrets in `application.properties` |
| Medium | 2 | Deprecated `WebMvcConfigurerAdapter` (removed in Spring 6), no Actuator hardening |
| Low | 1 | `aws-sdk` 2.25.13 — older than current; transitive CVE risk in Netty / Jackson |

## Top findings

### CRIT-1 — Spring Boot 2.7.x EOL
- `spring-boot-starter-parent` 2.7.18 is the final 2.x release; OSS support ended **2025-11-30**.
- No further CVE patches will be issued upstream. Any future Spring framework CVE will leave this app exposed.
- Mitigation in Phase 4: upgrade to Spring Boot 3.3.x (current LTS-train).

### CRIT-2 — JDK 8
- Public security updates from most vendors (Microsoft, Eclipse, Oracle's free track) have ended or are paid-only.
- Mitigation: JDK 21 (current LTS).

### HIGH-1 — Hardcoded AWS access key + secret
- `web/src/main/resources/application.properties`:
  - `aws.accessKey=your-access-key`
  - `aws.secretKey=your-secret-key`
- Symmetric pattern in `worker/`.
- Mitigation: Replace AWS SDK with Azure Storage Blob + **DefaultAzureCredential** (Managed Identity in Azure; developer credential locally). Remove access-key fields entirely.

### HIGH-2 — Hardcoded RabbitMQ credentials
- `spring.rabbitmq.username=guest`, `spring.rabbitmq.password=guest`.
- Mitigation: Migrate to **Azure Service Bus** with Managed Identity (no password at all).

### HIGH-3 — Hardcoded PostgreSQL password
- `spring.datasource.password=postgres`.
- Mitigation: **Azure Database for PostgreSQL Flexible Server + azure-identity-extensions** for token-based passwordless auth.

### HIGH-4 — Secrets in source control
- All three credential sets live in `application.properties` committed to git.
- Mitigation: Move to **Azure Key Vault** + Spring Cloud Azure Key Vault config source.

### MED-1 — Deprecated Spring API
- `WebMvcConfigurerAdapter` and `HandlerInterceptorAdapter` are removed in Spring 6 / Boot 3 — this blocks the upgrade path.
- Fix in Phase 4: implement `WebMvcConfigurer` and `HandlerInterceptor` directly.

### MED-2 — No health probes
- No `spring-boot-starter-actuator` dependency. Container Apps probes would have nothing to call.
- Fix in Phase 4: add actuator + expose `health`, `info`.

## Acceptance criteria for Phase 5 (security delta)

- 0 hardcoded cloud credentials in modernized tree.
- 0 EOL Spring Boot / JDK majors.
- Health endpoints exposed.
- Modernized tree builds under JDK 21 with `-Werror`-equivalent (no compile errors).
