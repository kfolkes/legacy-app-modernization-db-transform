# Phase 2 — Security Baseline (Java)

> Cross-validated by: dependency manifest review + Spring/Hibernate EOL data + source scan for hardcoded credentials.

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
