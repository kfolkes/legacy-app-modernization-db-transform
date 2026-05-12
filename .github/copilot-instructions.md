# Copilot Instructions — App Modernization Lab

## What this repo is

A reusable accelerator that uses GitHub Copilot agents to modernize legacy applications into Azure-ready, current-stack apps. Two languages, one pipeline:

- **.NET Framework → .NET 10** via `/dotnet.modernize`
- **Java 8/11/17 → Java 21 + Spring Boot 3** via `/java.modernize`

Both share the same 8-phase pipeline, devcontainer, and evidence-doc contract. Only the toolchain differs.

## Repo layout

```
.github/                                  # Orchestration (agents, skills, prompts)
├── agents/                               # Chat agents (one per stack + optional PM)
├── skills/dotnet-modernization-flow/     # .NET single source of truth
├── skills/java-modernization-flow/       # Java single source of truth
└── prompts/                              # /dotnet.modernize, /java.modernize
legacy/
├── dotnet-eshop/                         # .NET Framework sample (eShopModernizing)
└── java-asset-manager/                   # Java sample from java-migration-copilot-samples
modernized/
├── dotnet-eshop/                         # .NET 10 output
└── java-asset-manager/                   # Java 21 + Spring Boot 3 output
docs/
├── dotnet/                               # .NET phase evidence (01-08)
└── java/                                 # Java phase evidence (01-08)
sec-check/                                # Security scanner used in phases 2 + 5
templates/                                # Optional scaffolds (microservice / BFF / Aspire)
scripts/                                  # Headless helpers + CI entry points
```

**Key rule:** Orchestration lives in `.github/`. Generated evidence docs live in `docs/<stack>/`. Never mix orchestration content into result-doc folders.

## The 8-phase pipeline

Single source of truth per stack:
- `.github/skills/dotnet-modernization-flow/SKILL.md`
- `.github/skills/java-modernization-flow/SKILL.md`

Both flows have identical phases:

| Phase | Output |
|---|---|
| 0. Precheck | — |
| 1. Assessment | `docs/<stack>/01-legacy-assessment.md` |
| 2. Security baseline | `docs/<stack>/02-security-baseline.md` |
| 3. Modernization plan | `docs/<stack>/03-modernization-plan.md` |
| 4. Implementation | source code in `modernized/<stack>/` |
| 5. Security revalidation | `docs/<stack>/05-security-comparison.md` |
| 6. Build + test validation | — |
| 7. Architecture documentation | `docs/<stack>/07-architecture-documentation.md` |
| 8. Deployment plan | `docs/<stack>/08-deployment-plan.md` |

## Critical conventions

- **Never hardcode the source version.** Always detect from `*.csproj` / `packages.config` / `global.json` / `*.sln` (.NET) or `pom.xml` / `build.gradle` / `.java-version` (Java).
- **Always target current stable:** `net10.0` for .NET, JDK 21 + Spring Boot 3.3 for Java.
- **Always merge ≥2 tools per critical phase** — no single tool decides assessment, plan, or validation outcomes.
- **Modernized code patterns must include `// SECURITY FIX:` and `// MIGRATION:` comments** explaining what changed and why.
- **Default Azure target is Container Apps** unless the customer specifies otherwise.

## Modernized .NET defaults

- SDK-style `*.csproj`
- Minimal hosting `Program.cs` (replaces `Global.asax`)
- Built-in DI (replaces Autofac)
- Serilog (replaces log4net)
- EF Core with HiLo (replaces EF6)
- Async + `CancellationToken`
- User Secrets (replaces `Web.config`)

## Modernized Java defaults

- JDK 21
- Spring Boot 3.3 (`javax.*` → `jakarta.*`)
- Jakarta EE 10 (where applicable)
- JUnit 5
- SLF4J + Logback
- Spring Data JPA
- Azure Service Bus (replaces RabbitMQ / on-prem MQ)
- Azure Blob (replaces local file storage)
- Key Vault + Managed Identity (replaces hardcoded secrets)

## Key commands

```bash
# One-click from Copilot Chat
/dotnet.modernize                                            # default sample
/dotnet.modernize legacy/dotnet-eshop/eShopLegacyMVCSolution # explicit
/java.modernize                                              # default sample
/java.modernize legacy/java-asset-manager                    # explicit

# Build modernized output
dotnet build modernized/dotnet-eshop/eShopModernized.sln
mvn -f modernized/java-asset-manager/pom.xml verify
```

## When editing agents, skills, or prompts

- Agent definitions (`.github/agents/*.agent.md`) declare tool access — keep `tools:` frontmatter in sync with what the agent actually uses.
- Skill files (`.github/skills/*/SKILL.md`) use `requires.agents` and `requires.tools` frontmatter — update when adding tool dependencies.
- Prompt files (`.github/prompts/*.prompt.md`) bind to a specific agent via `agent:` frontmatter and define output paths under `docs/<stack>/`.

## sec-check integration

`sec-check/` is a Python multi-scanner used in Phases 2 and 5 for both stacks. It has its own `copilot-instructions.md` at `sec-check/.github/copilot-instructions.md`. Follow that subproject's conventions when editing sec-check itself.

## Optional templates

`templates/` contains optional scaffolds (Aspire orchestrator, .NET microservice, Kotlin BFF). They are not required by either core flow — use them only when Phase 3 of the plan recommends microservice decomposition or a BFF.
