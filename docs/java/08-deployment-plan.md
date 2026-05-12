# Phase 8 — Deployment Plan (Java → Azure Container Apps)

## Target topology

- **Resource group:** `rg-appmod-lab`
- **Region:** `eastus2`
- **Container Apps environment:** `cae-appmod-lab`
- **Apps:**
  - `web` — public ingress, HTTPS 443
  - `worker` — internal only, no ingress
- **Backing services:**
  - **Azure Database for PostgreSQL Flexible Server** (`pgflex-appmod-lab`) — Entra-only auth
  - **Azure Service Bus** namespace (`sb-appmod-lab`) — queue: `image-processing` (+ DLQ `image-processing.retry`)
  - **Azure Storage Account** (`stappmodlab`) — container `images`
  - **Azure Key Vault** (`kv-appmod-lab`) — connection strings + DB Entra group
- **Identity:** one **User-Assigned Managed Identity** (`uami-appmod-lab`) attached to both apps; granted `Storage Blob Data Contributor`, `Azure Service Bus Data Owner`, and PG `azure_ad_admin` group membership.

## Suggested azd layout

```
infra/
├── main.bicep            # rg + cae + all resources
├── modules/
│   ├── containerapps.bicep
│   ├── postgres.bicep
│   ├── servicebus.bicep
│   ├── storage.bicep
│   └── keyvault.bicep
azure.yaml                # azd manifest, two services (web, worker)
```

## Container build

Each module ships its own `Dockerfile`:

```dockerfile
# syntax=docker/dockerfile:1
FROM eclipse-temurin:21-jdk-jammy AS build
WORKDIR /src
COPY pom.xml /src/
COPY ../pom.xml /parent/
COPY src /src/src
RUN ./mvnw -B -DskipTests package || mvn -B -DskipTests package
FROM eclipse-temurin:21-jre-jammy
COPY --from=build /src/target/*.jar /app/app.jar
USER 65532:65532
EXPOSE 8080
ENTRYPOINT ["java","-XX:+UseContainerSupport","-jar","/app/app.jar"]
```

> Generated `Dockerfile` files are in `modernized/java-asset-manager/{web,worker}/Dockerfile`.

## Probes (Container Apps)

| Probe | Endpoint | Initial delay | Period |
|---|---|---|---|
| Liveness | `/actuator/health/liveness` | 30s | 10s |
| Readiness | `/actuator/health/readiness` | 10s | 5s |

## Rollout

1. **Provision** infra with `azd up` (single command).
2. **Build + push** images to the env's built-in registry via `azd deploy`.
3. **Smoke** — `curl https://<web-fqdn>/actuator/health` returns `UP`.
4. **Traffic split** — 100% to new revision once probes are green; previous revision retained at 0% for fast rollback (`az containerapp revision activate --weight ...`).

## Rollback

- `az containerapp revision set-mode --mode multiple` is already on by default in the suggested config; switching traffic back is a single command.
- DB schema changes use Flyway (introduced in Phase 4b); Flyway repeat-safe migrations enable forward-only rollout.

## Open follow-ups (Phase 4b)

- [ ] Wire `BlobServiceClient` + `DefaultAzureCredential` into `web` and `worker` (replace AWS SDK calls).
- [ ] Swap `@RabbitListener` for Service Bus `@ServiceBusListener`.
- [ ] Add `azure-identity-extensions` for PG passwordless.
- [ ] Bind Key Vault config source.
- [ ] Add bicep + `azure.yaml`.
