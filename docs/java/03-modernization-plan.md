# Phase 3 — Modernization Plan (Java)

> Merges: OpenRewrite Spring Boot 3 recipe set + official Spring Boot 3 migration guide + awesome-copilot Java patterns + Azure-Samples/java-migration-copilot-samples (asset-manager workshop).

## Target stack

| Layer | From | To |
|---|---|---|
| JDK | 8 | **21** (current LTS) |
| Spring Boot | 2.7.18 | **3.3.5** (current 3.3 LTS-train) |
| Jakarta namespace | `javax.*` | **`jakarta.*`** (Jakarta EE 10) |
| Servlet | Servlet 3.x | Servlet 6 (via Jakarta EE 10) |
| Hibernate | 5.x | 6.5 (transitively, via SB 3.3) |
| Storage | AWS S3 + access keys | **Azure Blob Storage** + Managed Identity (DefaultAzureCredential) |
| Messaging | RabbitMQ + password | **Azure Service Bus** + Managed Identity |
| Database | PostgreSQL + password | **Azure Database for PostgreSQL Flexible Server** + Entra passwordless |
| Secrets | `application.properties` | **Azure Key Vault** |
| Observability | none | Spring Boot Actuator (`/actuator/health`, `/actuator/info`) |
| Packaging | jar | **Docker multi-stage** (`eclipse-temurin:21-*`) |
| Deploy target | none | **Azure Container Apps** + Managed Identity |

## Changes per file (Phase 4 scope)

### Parent `pom.xml`
- `<parent>spring-boot-starter-parent</parent>`: `2.7.18` → `3.3.5`.
- `<java.version>`: `8` → `21`.
- Add `<maven.compiler.source>`, `<maven.compiler.target>` = `21`.

### `web/pom.xml`, `worker/pom.xml`
- Add `spring-boot-starter-actuator`.
- Add Azure SDK BOM (`com.azure:azure-sdk-bom:1.2.27`).
- Add `com.azure:azure-storage-blob`, `com.azure:azure-identity`.
- Keep `software.amazon.awssdk:s3` (transitional; Phase 4b removes it).

### Java source — `javax.*` → `jakarta.*`
Affected files (5 total):
- `web/.../config/WebMvcConfig.java`
- `web/.../model/ImageMetadata.java`
- `web/.../service/LocalFileStorageService.java`
- `worker/.../model/ImageMetadata.java`
- `worker/.../service/LocalFileProcessingService.java`

### `web/.../config/WebMvcConfig.java` — Spring API removals
- `WebMvcConfigurerAdapter` → implement `WebMvcConfigurer` directly.
- `HandlerInterceptorAdapter` → implement `HandlerInterceptor` directly.
- Add `// MIGRATION:` comment block at top.

### Phase 4b (stretch — not required for compile success)
- Replace `AwsS3Service`/`S3FileProcessingService` with `AzureBlobStorageService` using `BlobServiceClientBuilder().credential(new DefaultAzureCredentialBuilder().build())`.
- Replace `@RabbitListener` with `ServiceBusProcessorClient` (or `@ServiceBusListener` from Spring Cloud Azure).
- Add `azure-identity-extensions` for PG passwordless.
- Add Key Vault config source.

## Build-validation checkpoints

1. After parent pom edit → `mvn validate` passes.
2. After javax sweep → `mvn -DskipTests compile` passes for `web` (the harder module).
3. After both modules edited → reactor `clean compile` passes.

## Risks + mitigations

| Risk | Mitigation |
|---|---|
| Lombok incompatible with JDK 21 | Spring Boot 3.3.5 manages Lombok 1.18.34 which supports JDK 21 ✅ |
| Hibernate 6 dialect renames | `org.hibernate.dialect.PostgreSQLDialect` is still valid in Hib 6 ✅ |
| `WebSecurityConfigurerAdapter` removal | Not used in this sample (no Spring Security yet) ✅ |
| Spring Boot Actuator default-exposed endpoints | Limit via `management.endpoints.web.exposure.include=health,info` ✅ |
| AWS SDK v2 on JDK 21 | Bumped to `2.27.21` to pick up latest patch set ✅ |
