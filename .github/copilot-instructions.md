# Copilot Instructions — sbux-appmod-demo

## What This Repo Is

A **demo workspace for end-to-end .NET Framework → .NET 10 modernization** using GitHub Copilot agents, skills, and tools. It showcases an 8-phase modernization pipeline applied to the `eShopModernizing` sample app (.NET Framework 4.7.2 → net10.0).

## Workspace Layout (4 Git Repos + Orchestration)

```
.github/                        # Orchestration layer (agents, skills, prompts)
├── agents/                     # Chat agents (appmodernization-dotnet, pm-migration-agent)
├── skills/                     # Reusable skill definitions (dotnet-modernization, dotnet10-modernization-customer)
└── prompts/                    # One-click replay prompts (dotnet10.modernize*)
legacy/                         # Source: eShopModernizing (.NET Framework 4.7.2, MVC 5, EF6, Autofac, log4net)
modernized/                     # Target: eShopModernized (net10.0, EF Core, built-in DI, Serilog, OpenTelemetry)
hve-core/                       # HVE Core framework (RPI methodology, task-researcher/task-planner/rpi-agent)
sec-check/                      # Security scanner agent (@sechek.security-scanner, Python CLI + Copilot toolkit)
versions/agent-upgrades-v1/     # Versioned replay output (docs/, modernized/, logs/)
docs/                           # Generated phase result documents (01 through 08)
```

**Key rule:** Orchestration lives in `.github/`. Generated evidence docs live in `docs/` or `versions/*/docs/`. Never mix orchestration content into result doc folders.

## The 8-Phase Pipeline

The single source of truth is `.github/skills/dotnet10-modernization-customer/SKILL.md`. Every phase combines three input sources:

| Source | Role |
|---|---|
| **HVE Core agents** (`task-researcher`, `task-planner`, `rpi-agent`) | Qualitative analysis via RPI methodology |
| **AppMod-Dotnet tools** (`appmod-dotnet-*`) | Objective assessment, CVE checks, build/test validation |
| **awesome-copilot + dotnet-upgrade** | Proven migration patterns, package replacement recipes |

**Always merge findings from all three sources.** Phase 3 (Modernization Plan) must explicitly list patterns from both awesome-copilot AND the dotnet-upgrade tool.

### Result Docs Contract

| File | Phase |
|---|---|
| `docs/01-legacy-assessment.md` | Phase 1: Legacy Assessment |
| `docs/02-security-baseline.md` | Phase 2: Security Baseline |
| `docs/03-modernization-plan.md` | Phase 3: Modernization Plan |
| `docs/05-security-comparison.md` | Phase 5: Security Revalidation |
| `docs/07-architecture-documentation.md` | Phase 7: Architecture Docs |
| `docs/08-deployment-plan.md` | Phase 8: Deployment Plan |

## Critical Conventions

- **Never hardcode or assume the source .NET Framework version.** Always detect it from `*.csproj`, `packages.config`, `global.json`, or `*.sln` metadata.
- **Always target `net10.0`** for the modernized output.
- **Modernized code patterns:** SDK-style csproj, minimal hosting `Program.cs` (replaces `Global.asax`), built-in DI (replaces Autofac), Serilog (replaces log4net), EF Core with HiLo sequences (replaces EF6), async with `CancellationToken` throughout, User Secrets for connection strings (replaces `Web.config`).
- **Security comments in code:** Modernized files include `// SECURITY FIX:` and `// MIGRATION:` comments explaining what changed and why. Preserve this convention when editing.

## Key Commands

```powershell
# Reset the versioned replay workspace before a new run
.\versions\agent-upgrades-v1\run-replay.ps1

# One-click modernization via Copilot Chat
# /dotnet10.modernize.agent-upgrades-v1 legacy/eShopLegacyMVCSolution

# Build modernized app
dotnet build modernized/eShopModernized.sln

# Run tests
dotnet test modernized/tests/eShopModernized.Tests/
```

## When Editing Agents, Skills, or Prompts

- Agent definitions (`.github/agents/*.agent.md`) declare tool access and execution rules — keep the `tools:` frontmatter in sync with what the agent actually uses.
- Skill files (`.github/skills/*/SKILL.md`) use `requires.agents` and `requires.tools` frontmatter — update these when adding new tool dependencies.
- Prompt files (`.github/prompts/*.prompt.md`) bind to a specific agent via `agent:` frontmatter and define output paths — `versions/agent-upgrades-v1/` for replay, `docs/` for default runs.

## sec-check Integration

sec-check is a Python-based security scanner with its own copilot instructions at `sec-check/.github/copilot-instructions.md`. It provides the `@sechek.security-scanner` agent used in Phases 2 and 5. When modifying sec-check itself, follow that subproject's conventions (Python 3.12, venv, Copilot SDK).
