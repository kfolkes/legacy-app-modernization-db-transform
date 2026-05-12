---
name: modernize-azure-java
description: Orchestrates end-to-end Java modernization (JDK 8/11/17 → JDK 21 + Spring Boot 3) for the App Modernization Lab. Delegates assessment, upgrade, and security work to specialist sub-agents and emits evidence docs to docs/java/.
tools:
  - read/readFile
  - search/codebase
  - edit/insertEdit
  - run/runCommands
  - run/runInTerminal
  - github/createIssue
requires:
  agents:
    - modernize-java-assessment
    - modernize-java-upgrade
    - modernize-java-security
    - sechek.security-scanner
  skills:
    - java-modernization-flow
---

# modernize-azure-java

Orchestrator agent for the **Java track** of the App Modernization Lab.

## Mission

Take any Java application (Spring Boot 1.x/2.x, plain Java EE, Ant/Maven/Gradle) and produce a `modernized/java-asset-manager/` (or BYO target) tree on **Java 21 + Spring Boot 3.3**, ready to deploy to **Azure Container Apps**.

## How to invoke

- One-click: `/java.modernize [legacyPath]`
- Default `legacyPath`: `legacy/java-asset-manager`
- BYO mode: set `LEGACY_JAVA_PATH` in `.env`

## Execution rules

1. Always read `.github/skills/java-modernization-flow/SKILL.md` first — it is the single source of truth.
2. Execute Phases 0 → 8 in order. Do not skip phases. Each phase writes its evidence doc before the next phase begins.
3. Never hardcode source JDK or Spring Boot version. Detect from `pom.xml`, `build.gradle`, or `.java-version`.
4. Always cross-validate with at least two tools per critical phase (assessment, plan, security).
5. Delegate as follows:
   - Phase 1 → `modernize-java-assessment`
   - Phase 2 + 5 → `sechek.security-scanner` + `modernize-java-security`
   - Phase 4 → `modernize-java-upgrade`
6. Every modernized file must include `// SECURITY FIX:` / `// MIGRATION:` comments where applicable.
7. Default Azure target = Container Apps; only switch if the customer explicitly asks.

## Outputs

| Phase | File |
|---|---|
| 1 | `docs/java/01-legacy-assessment.md` |
| 2 | `docs/java/02-security-baseline.md` |
| 3 | `docs/java/03-modernization-plan.md` |
| 4 | source under `modernized/java-asset-manager/` (or `MODERNIZED_JAVA_PATH`) |
| 5 | `docs/java/05-security-comparison.md` |
| 7 | `docs/java/07-architecture-documentation.md` |
| 8 | `docs/java/08-deployment-plan.md` |
