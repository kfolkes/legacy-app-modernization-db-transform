---
name: java.modernize
description: One-click Java modernization to Java 21 + Spring Boot 3 + Azure. Drives the GitHub Copilot App Modernization for Java extension (vscjava.migrate-java-to-azure) and runs its Predefined Tasks (PostgreSQL Flex, Blob, Service Bus, Health Endpoints, Containerize, Deploy).
agent: modernize-azure-java
argument-hint: "[legacyPath] e.g. legacy/java-asset-manager OR leave blank for default sample"
tools: ['read/readFile', 'search/codebase', 'search/fileSearch', 'search/textSearch', 'search/listDirectory', 'todo', 'agent', 'execute', 'edit', 'search']
---

# One-Click Java → Java 21 + Azure Modernization

Read the skill file `.github/skills/java-modernization-flow/SKILL.md` first — it is the single source of truth for orchestration and lists the exact extension tasks to invoke at each phase.

## Mode Selection

If `legacyPath` matches a folder under `legacy/java-asset-manager/` → **Demo mode** (bundled asset-manager sample).
Otherwise → **BYO mode** against the customer-provided Java source.

Source path argument: `${input:legacyPath:legacy/java-asset-manager}`

## Required Outcome

Execute phases 0–8 end-to-end via the **App Modernization for Java extension** (`vscjava.migrate-java-to-azure`). Result docs go to `docs/java/` only.

### Phase Workflow (extension-task driven)

1. **Phase 0 — Precheck**
   - **Reset demo state first**: run `bash scripts/reset-demo.sh java` to delete every file under `docs/java/` except `README.md`. Never reuse pre-existing evidence docs — the demo MUST regenerate them from scratch each run so phases reflect the live tool output, not a previous session. Also re-verify the modernized source tree against the legacy source rather than trusting the contents of `modernized/java-asset-manager/`.
   - Detect JDK + framework from `pom.xml` / `build.gradle` / `.java-version`.
   - Verify the App Modernization for Java extension is installed.
   - Verify Java 21 + Maven/Gradle + Docker available.

2. **Phase 1 — Assessment** → `docs/java/01-legacy-assessment.md`
   - Invoke `@modernize-java-assessment` (extension agent) + OpenRewrite scan.
   - Record baseline `mvn clean compile` on source JDK.

3. **Phase 2 — Security baseline** → `docs/java/02-security-baseline.md`
   - Invoke `@modernize-java-security` (extension CVE scan) + sec-check + OWASP dep-check.

4. **Phase 3 — Plan** → `docs/java/03-modernization-plan.md`
   - Merge extension recommendations + Spring Boot 3 migration guide + awesome-copilot Java patterns.
   - Enumerate every Predefined Task that Phase 4 will run (4b–4f).

5. **Phase 4 — Implementation** (extension Predefined Tasks, in order):
   - **4a** `@modernize-java-upgrade` — JDK + Spring Boot upgrade (javax → jakarta sweep, Spring 6 API fixes).
   - **4b** Predefined Task: **"Migrate to Azure Database for PostgreSQL Flexible Server"** — passwordless via `azure-identity-extensions`.
   - **4c** Predefined Task: **"Migrate to Azure Blob Storage"** — `azure-storage-blob` + `azure-identity` (DefaultAzureCredential), removes AWS S3 / local files.
   - **4d** Predefined Task: **"Migrate to Azure Service Bus"** — `spring-cloud-azure-starter-servicebus`, removes RabbitMQ.
   - **4e** Custom Skill: **"Expose Health Endpoints"** — adds `spring-boot-starter-actuator`.
   - **4f** Predefined Task: **"Containerize Applications"** — multi-stage Dockerfile on `eclipse-temurin:21`.
   - Add `// SECURITY FIX:` / `// MIGRATION:` comments at every change.
   - Validate with `mvn verify` after each task.

6. **Phase 5 — Security delta** → `docs/java/05-security-comparison.md`
   - Re-run extension CVE scan + sec-check; produce before/after table.

7. **Phase 6 — Build + Test**
   - `mvn -DskipTests clean compile` → BUILD SUCCESS on JDK 21.
   - `mvn verify` → all tests green.
   - Optional: trigger `modernization-integration-tests` skill (Layers 1–2 local; 3–4 require Azure).

8. **Phase 7 — Architecture** → `docs/java/07-architecture-documentation.md`
   - Before/after Mermaid + migration map citing which Predefined Task produced each change.

9. **Phase 8 — Deploy** → `docs/java/08-deployment-plan.md`
   - Predefined Task: **"Deploy to Azure"** (default = Azure Container Apps).
   - Capture generated `azure.yaml` + bicep outline + MI permissions.

## Guardrails

- Never assume source JDK or framework version — always detect.
- Always target Java 21 + Spring Boot 3.x.
- Never skip a Predefined Task in Phase 4 — that is the customer-recommended Azure migration path the extension provides.
- Only write evidence to `docs/java/`. No orchestration in `docs/java/`.

## Final Response Format

1. Detected source JDK + framework + app type
2. Phase 0–8 status with the **extension task** invoked at each phase
3. Security delta (before → after)
4. Build/test status (legacy reference + modernized)
5. Files changed by each Predefined Task
6. Azure target + open risks + next manual step
