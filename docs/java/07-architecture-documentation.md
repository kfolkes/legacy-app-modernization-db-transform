# Phase 7 — Architecture (Java)

## Legacy (before)

```mermaid
flowchart LR
  User([User]) -->|HTTP| Web[Web<br/>Spring Boot 2.7 / JDK 8]
  Web -->|JDBC + password| PG[(PostgreSQL)]
  Web -->|AMQP + guest/guest| MQ([RabbitMQ<br/>image-processing])
  Web -->|AWS SDK + access key| S3[(AWS S3)]
  MQ --> Worker[Worker<br/>Spring Boot 2.7 / JDK 8]
  Worker --> PG
  Worker --> S3
```

## Modernized (after)

```mermaid
flowchart LR
  User([User]) -->|HTTPS| ACA1[Container App: web<br/>JDK 21 / Spring Boot 3.3]
  ACA1 -->|Entra token| PGFlex[(Azure Database for<br/>PostgreSQL Flexible Server)]
  ACA1 -->|Managed Identity| SB([Azure Service Bus<br/>image-processing])
  ACA1 -->|Managed Identity| Blob[(Azure Blob Storage)]
  ACA1 -.->|secrets ref| KV[(Azure Key Vault)]
  SB --> ACA2[Container App: worker<br/>JDK 21 / Spring Boot 3.3]
  ACA2 -->|Managed Identity| PGFlex
  ACA2 -->|Managed Identity| Blob
  ACA2 -.->|secrets ref| KV

  subgraph Probes
    direction TB
    P1[/actuator/health/]
    P2[/actuator/info/]
  end
  ACA1 -.-> Probes
  ACA2 -.-> Probes
```

## Migration map

| Legacy component | Modernized replacement |
|---|---|
| Tomcat (embedded) on Spring Boot 2.7 | Tomcat (embedded) on Spring Boot 3.3 |
| `javax.servlet.*` | `jakarta.servlet.*` |
| `javax.persistence.*` | `jakarta.persistence.*` |
| `WebMvcConfigurerAdapter` | `WebMvcConfigurer` (interface) |
| `HandlerInterceptorAdapter` | `HandlerInterceptor` (interface) |
| Hibernate 5 | Hibernate 6.5 |
| AWS S3 SDK v2 + access keys | Azure Storage Blob SDK + DefaultAzureCredential |
| RabbitMQ AMQP + guest password | Azure Service Bus + Managed Identity |
| PostgreSQL password | Azure Database for PostgreSQL Flexible Server + Entra token |
| `application.properties` secrets | Azure Key Vault (+ Spring Cloud Azure config source) |
| (none) | Spring Boot Actuator (`/actuator/health`, `/actuator/info`) |
| (none) | Multi-stage Dockerfile (`eclipse-temurin:21`) |
| (none) | Azure Container Apps |
