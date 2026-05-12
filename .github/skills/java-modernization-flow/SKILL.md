---
name: java-modernization-flow
description: One-click reusable Java modernization flow that drives the GitHub Copilot App Modernization for Java extension (vscjava.migrate-java-to-azure) end-to-end. Detects source JDK and framework, runs the extension's predefined Azure migration tasks (PostgreSQL Flex, Blob, Service Bus, Health Endpoints), modernizes to Java 21 + Spring Boot 3, validates, and produces an Azure deployment plan.
version: 1.1.0
author: AppMod Team
maturity: stable
requires:
  agents:
    - modernize-azure-java
    - modernize-java-upgrade
    - modernize-java-security
    - modernize-java-assessment
    - sechek.security-scanner
  extensions:
    - vscjava.migrate-java-to-azure   # App Modernization for Java (predefined Azure tasks)
  tools:
    - read_file
    - file_search
    - search/codebase
trigger_phrases:
  - "modernize java app"
  - "upgrade java to 21"
  - "java modernization flow"
  - "one click java upgrade"
  - "spring boot 2 to 3"
  - "java ee to jakarta ee"
  - "migrate java app to azure"
---

# Java → Java 21 + Azure Modernization Flow

Single source of truth for the Java modernization pipeline. Mirrors the .NET flow phase-for-phase. All orchestration lives here. `docs/java/` only holds generated results.

This flow is **driven by the GitHub Copilot App Modernization for Java extension** (`vscjava.migrate-java-to-azure`). Each Azure migration step is delegated to that extension's **Predefined Tasks** so customers get the same in-product experience the extension exposes via its sidebar.

---

## When to Use

- Source is any JDK 8+ application (Spring Boot 1.x/2.x, Java EE, Jakarta EE, plain Maven/Gradle).
- Target is Java 21 + Spring Boot 3.x (or Jakarta EE 10) + Azure-ready.
- Need a repeatable, audit-ready process backed by Microsoft's App Modernization for Java tooling.

## Operating Modes

### Mode A — Demo (bundled sample)
- Source: `legacy/java-asset-manager/` (asset-manager workshop from Azure-Samples/java-migration-copilot-samples).
- Target: `modernized/java-asset-manager/` running on Azure Container Apps with Postgres + Service Bus + Blob.

### Mode B — BYO Codebase
- Source: any `pom.xml` / `build.gradle` tree pointed to via `legacyPath`.
- Target: `modernized/<inferred-name>/`.

---

## Inputs

| Input | Required | Default |
|---|---|---|
| `legacyPath` | Yes | `legacy/java-asset-manager` |
| `targetJdk` | No | `21` |
| `targetSpringBoot` | No | `3.3.x` |
| `azureTarget` | No | `container-apps` |
| `outputDir` | No | `modernized/<auto>/` |

---

## Extension-driven phase map

Every phase calls one or more **App Modernization for Java extension** features. The extension exposes them as Predefined Tasks (sidebar) and as Copilot agents (`@modernize-java-*`).

| Phase | Extension feature invoked | Sub-agent / task |
|---|---|---|
| 1 Assessment | `@modernize-java-assessment` (extension agent) + OpenRewrite scan | "Run assessment" task |
| 2 Security baseline | `@modernize-java-security` (extension agent) + sec-check | "Run CVE check" task |
| 3 Plan synthesis | extension recommendations + Spring Boot 3 migration guide | (planning, no task) |
| 4a Java + Spring upgrade | `@modernize-java-upgrade` (extension agent) | "Upgrade JDK + Spring Boot" predefined task |
| 4b Migrate DB → Azure | extension predefined task | **"Migrate to Azure Database for PostgreSQL Flexible Server"** |
| 4c Migrate storage → Azure | extension predefined task | **"Migrate to Azure Blob Storage"** |
| 4d Migrate messaging → Azure | extension predefined task | **"Migrate to Azure Service Bus"** |
| 4e Health endpoints | extension custom skill | **"Expose health endpoints"** |
| 4f Containerize | extension predefined task | **"Containerize Applications"** |
| 5 Security delta | re-run `@modernize-java-security` | (same as Phase 2) |
| 6 Build + test | `mvn verify` (extension also offers integration test layers) | `modernization-integration-tests` skill (Layer 1–4) |
| 7 Architecture | summarize what extension changed | (no task) |
| 8 Deployment | extension predefined task | **"Deploy to Azure"** (Container Apps) |

> All five **Predefined Tasks** are first-class features of the extension and appear in its sidebar. The orchestrator must invoke them in this exact order — the extension stores intermediate state per task in `.github/java-upgrade/` (auto-managed).

---

## Core Philosophy — Iterate to Consensus

Every critical step is validated by 2–3 independent tools.

| Step | Tool 1 | Tool 2 | Tool 3 |
|---|---|---|---|
| JDK + framework detection | `pom.xml`/`build.gradle` parse | `.java-version`/`Dockerfile` | bytecode major version |
| Assessment | `@modernize-java-assessment` (extension) | OpenRewrite recipe scan | awesome-copilot Java analyzer |
| Security baseline | sec-check | `@modernize-java-security` CVE scan | OWASP dep-check |
| Plan synthesis | extension recommendations | Spring Boot 3 migration guide | awesome-copilot Java patterns |
| Implementation | extension Predefined Tasks (4b–4f) | OpenRewrite execution | awesome-copilot templates |
| Build validation | `mvn verify` / `gradle build` | container build | dependency tree diff |
| Test validation | `mvn test` | TestContainers (Layer 1) + Smoke (Layer 2) + Azure Integration (Layer 3) + Behavioral (Layer 4) | parity check |
| Security revalidation | sec-check delta | extension CVE delta | OWASP delta |
| Azure readiness | extension "Deploy to Azure" task | Bicep/azd what-if | Container Apps probe |

---

## Phase-by-Phase

### Phase 0 — Precheck
1. Detect JDK + framework from `pom.xml`, `build.gradle`, or `.java-version`.
2. Verify the **App Modernization for Java extension** (`vscjava.migrate-java-to-azure`) is installed; if not, prompt user to install (or surface it via the devcontainer's `customizations.vscode.extensions`).
3. Verify Java 21 + Maven/Gradle + Docker available.
4. Confirm output dir is empty or new.

### Phase 1 — Legacy Assessment
**Output:** `docs/java/01-legacy-assessment.md`
- Invoke **extension agent**: `@modernize-java-assessment`.
- Cross-validate with OpenRewrite recipe scan + awesome-copilot Java analyzer.
- Detect framework features: Spring (version), Java EE / Jakarta EE, JPA/Hibernate, JMS/RabbitMQ, JAX-RS, JSP/Servlets, build system, javax vs jakarta.
- Record baseline `mvn clean compile` result on the source JDK as a reference build.

### Phase 2 — Security Baseline
**Output:** `docs/java/02-security-baseline.md`
- Invoke **extension agent**: `@modernize-java-security` (CVE scan).
- Run `sec-check` (SAST + secrets + deps).
- Run OWASP dependency-check.
- Record severity counts and top issues.

### Phase 3 — Modernization Plan
**Output:** `docs/java/03-modernization-plan.md`
- Merge extension recommendations + Spring Boot 3 migration guide + awesome-copilot Java patterns.
- Map each Phase-2 finding to a fix step.
- Standard patterns: JDK 8/11/17 → 21, Spring Boot 2.x → 3.3.x, javax → jakarta, Java EE → Jakarta EE 10, JUnit 4 → 5, JDBC → JPA, on-prem MQ → Service Bus, files → Blob, secrets → Key Vault + MI.
- Plan must explicitly enumerate which **extension Predefined Tasks** will be run in Phase 4 (4b–4f).

### Phase 4 — Implementation (extension-driven)

#### 4a. JDK + Spring Boot upgrade
- Invoke **extension agent**: `@modernize-java-upgrade` with target `JDK 21` + `Spring Boot 3.3.x`.
- Apply javax → jakarta sweep across all `.java` files.
- Fix removed Spring APIs (`WebMvcConfigurerAdapter`, `HandlerInterceptorAdapter`, `WebSecurityConfigurerAdapter`).

#### 4b. Database → Azure Database for PostgreSQL Flexible Server
- Run **Predefined Task: "Migrate to Azure Database for PostgreSQL Flexible Server"**.
- Outcome: `azure-identity-extensions` added; password auth removed; managed-identity token plugin wired.

#### 4c. Storage → Azure Blob Storage
- Run **Predefined Task: "Migrate to Azure Blob Storage"**.
- Outcome: `azure-storage-blob` + `azure-identity` added; AWS SDK references replaced with `BlobServiceClient` + `DefaultAzureCredential`.

#### 4d. Messaging → Azure Service Bus
- Run **Predefined Task: "Migrate to Azure Service Bus"**.
- Outcome: `spring-cloud-azure-starter-servicebus` added; `@RabbitListener`/`RabbitTemplate` swapped for Service Bus equivalents.

#### 4e. Health endpoints
- Run **Custom Skill: "Expose health endpoints"** from the extension.
- Outcome: `spring-boot-starter-actuator` added; `/actuator/health` and `/actuator/info` exposed for Container Apps probes.

#### 4f. Containerize
- Run **Predefined Task: "Containerize Applications"**.
- Outcome: multi-stage `Dockerfile` per module on `eclipse-temurin:21-*`.

#### 4g. Comments
- Add `// SECURITY FIX:` and `// MIGRATION:` comments at every non-trivial change site (Java single-line `//` style).

### Phase 5 — Security Revalidation
**Output:** `docs/java/05-security-comparison.md`
- Re-run `@modernize-java-security` + sec-check + OWASP.
- Produce before/after delta. Critical/High targets: 0.

### Phase 6 — Build + Test Validation
- `mvn -B -DskipTests clean compile` must succeed under JDK 21 → modernized tree.
- `mvn -B verify` must succeed.
- Optional: invoke `modernization-integration-tests` skill for Layer 1 (TestContainers) and Layer 2 (Smoke). Layers 3–4 require Azure resources.

### Phase 7 — Architecture Documentation
**Output:** `docs/java/07-architecture-documentation.md`
- Before/after Mermaid diagrams.
- Migration map (legacy component → modernized replacement, citing which extension task produced each change).

### Phase 8 — Azure Deployment Plan
**Output:** `docs/java/08-deployment-plan.md`
- Run **Predefined Task: "Deploy to Azure"** (extension default target = **Azure Container Apps**).
- Capture the generated `azure.yaml` + `infra/` outline.
- Document MI permissions, probes, rollback strategy.

---

## Guardrails

- Never hardcode source JDK or framework version — always detect.
- Never skip an extension Predefined Task in Phase 4 — they form the customer-recommended Azure migration path.
- Always target Java 21 + Spring Boot 3.x.
- Only write evidence docs to `docs/java/`. Never put orchestration content in `docs/java/`.
- Always swap on-prem dependencies for Azure managed equivalents in Phase 4 via the extension's predefined tasks.

---

## Final Response Format

1. Detected source JDK + framework + app type
2. Per-phase status (1–8) with the extension task invoked at each phase
3. Security delta (before → after)
4. Build/test validation status (legacy reference build + modernized build)
5. Files changed by each Predefined Task
6. Azure target + open risks + next manual step (e.g. `azd up`)
