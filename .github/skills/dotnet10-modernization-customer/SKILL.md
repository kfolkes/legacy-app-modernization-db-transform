---
name: dotnet10-modernization-customer
description: Customer-ready reusable accelerator to run phases 1-8 of .NET modernization from unknown source versions to .NET 10 by combining HVE Core agents, AppMod-Dotnet tools, sec-check, awesome-copilot patterns, and the dotnet-upgrade tool.
version: 2.0.0
author: Starbucks AppMod Demo Team
maturity: stable
requires:
  agents:
    - task-researcher
    - task-planner
    - rpi-agent
    - appmodernization-dotnet
    - sechek.security-scanner
    - pm-migration-agent
  tools:
    - appmod-dotnet-install-appcat
    - appmod-dotnet-run-assessment
    - appmod-dotnet-build-project
    - appmod-dotnet-cve-check
    - appmod-dotnet-run-test
    - mssql_connect
    - mssql_list_tables
    - mssql_run_query
    - pgsql_migration_show_report
trigger_phrases:
  - "upgrade unknown .net app to .net 10"
  - "run app modernization phases 1-8"
  - "customer modernization accelerator"
  - "framework version unknown upgrade"
  - "modernize .net application"
  - "net framework to .net 10"
---

# Customer Reusable Skill — .NET to .NET 10

This is the **single source of truth** for running phases 1-8 of .NET modernization.
All orchestration lives here. `docs/` only holds generated results.

---

## When to Use

- Customer has a legacy .NET application and source framework version may be unknown.
- Customer wants a repeatable process with audit-ready artifacts.
- Customer requires security proof before and after modernization.

## Inputs

| Input | Required | Default |
|---|---|---|
| `legacyPath` | Yes | — |
| `targetFramework` | No | `net10.0` |
| `dbScriptsPath` | No | auto-detected |
| `deploymentConstraints` | No | none |

---

## Toolchain — Why Three Sources

| Source | Role | Strength |
|---|---|---|
| **HVE Core agents** | Structured RPI orchestration | Deep qualitative analysis and planning |
| **AppMod-Dotnet tools** | Deterministic assessment + build validation | Objective compatibility telemetry, CVE data |
| **awesome-copilot + dotnet-upgrade** | Proven migration patterns + automated upgrade | Package replacement recipes, startup/config/EF patterns |

**Rule:** Always use all three. HVE gives depth, AppMod gives objectivity, awesome-copilot + upgrade gives practical implementation patterns. Merge results from both awesome-copilot and the upgrade tool into the modernization plan.

---

## Phase-by-Phase Orchestration

### Phase 0: Precheck and Detect

**Tools:** `appmod-dotnet-install-appcat`, file system analysis

1. Ensure AppCAT is installed: `appmod-dotnet-install-appcat`.
2. Auto-detect source .NET version from `*.sln`, `*.csproj`, `packages.config`, `global.json`.
3. Identify app type (ASP.NET MVC, Web Forms, Web API, WPF, WinForms, Console).
4. Never hardcode or assume source version.

### Phase 1: Legacy Assessment

**Tools:** `@task-researcher`, `appmod-dotnet-run-assessment`
**Output:** `docs/01-legacy-assessment.md`

Run in parallel:
- HVE `task-researcher`: full architecture, dependency, business-rule inventory.
- AppMod assessment: `appmod-dotnet-run-assessment --project-path [legacyPath]`.
- awesome-copilot: baseline upgrade pathway patterns for detected source version.

Merge all three into a single assessment document containing:
- Application profile (type, framework, deployment model)
- Code inventory (controllers, services, repos, models, DI container, logging)
- Stored procedure analysis (inventory, business rules, SP → C# mapping)
- Dependency analysis (NuGet packages, CVEs, EOL, replacements)
- AppCAT compatibility report integration
- awesome-copilot upgrade pathway recommendation

### Phase 2: Security Baseline

**Tools:** `@sechek.security-scanner`, `appmod-dotnet-cve-check`
**Output:** `docs/02-security-baseline.md`

Run in parallel:
- sec-check: `@sechek.security-scanner` deep scan of legacy codebase.
- AppMod CVE check: `appmod-dotnet-cve-check --project-path [legacyPath] --include-transitive`.

Produce:
- Total findings by severity (Critical, High, Medium, Low)
- Overall security score (0-100)
- CVE details with CVSS scores
- Code-level issues (plaintext secrets, SQL injection, PII logging)
- Prioritized remediation list feeding into Phase 3

### Phase 3: Modernization Plan

**Tools:** `@task-planner`, awesome-copilot patterns, dotnet-upgrade tool
**Output:** `docs/03-modernization-plan.md`

**Critical rule:** Merge results from BOTH awesome-copilot patterns AND the upgrade tool into this plan.

1. Use `@task-planner` to create phased implementation plan.
2. Consult awesome-copilot `.NET upgrade patterns` for:
   - Package replacement matrix (e.g., Autofac → built-in DI, log4net → Serilog, EF6 → EF Core)
   - Startup modernization templates (Global.asax → Program.cs)
   - Configuration migration (Web.config → appsettings.json + User Secrets)
3. Run dotnet-upgrade analysis for automated upgrade recommendations.
4. Cross-reference AppMod assessment blockers from Phase 1.
5. Map which security findings each step resolves.

The generated plan must contain:
- Step-by-step migration sequence with estimated durations
- Security-fix mapping per step
- Risk mitigation checkpoints
- awesome-copilot patterns applied (explicitly listed)
- dotnet-upgrade recommendations applied (explicitly listed)
- Validation criteria per step

### Phase 4: Implementation

**Tools:** `@rpi-agent`, `appmod-dotnet-build-project`, awesome-copilot patterns
**No dedicated output doc** — produces the modernized codebase under `modernized/`

1. Execute the plan from Phase 3 using `@rpi-agent`.
2. Apply awesome-copilot implementation templates for .NET 10 idioms.
3. Key transformations:
   - SDK-style project file targeting `net10.0`
   - Minimal hosting `Program.cs`
   - EF Core with HiLo sequences and seed data
   - Stored procedure business logic → C# domain methods
   - Async throughout with `CancellationToken`
   - Security headers, health checks, structured logging
4. Validate build: `appmod-dotnet-build-project --project-path modernized/src/[AppName]`.

### Phase 4B: Database Migration (SQL Server → PostgreSQL)

**Tools:** `mssql_connect`, `mssql_list_tables`, `mssql_run_query`, `pgsql_migration_show_report`, EF Core Migrations
**Output:** `docs/04b-database-migration.md`

1. Inspect source SQL Server schema using MSSQL Extension tools.
2. Translate all T-SQL stored procedures to PL/pgSQL functions (migration intermediate).
3. Document all T-SQL → PL/pgSQL syntax differences (variable declaration, error handling, temp tables, transaction control, string functions, date functions, identity columns, and more).
4. Add `Npgsql.EntityFrameworkCore.PostgreSQL` package for dual DB provider support.
5. Update `Program.cs` with conditional `UseSqlServer()` / `UseNpgsql()` based on `DatabaseProvider` config.
6. Create `appsettings.PostgreSQL.json` with Azure PG Flexible Server + Entra ID passwordless auth.
7. Generate pgLoader configuration for production data migration.
8. Produce labeled SP mapping table (T-SQL → PL/pgSQL → C# EF Core) with Mermaid diagrams.
9. Define three verification methods: MSSQL Extension baseline, EF Core migration DDL comparison, unit test pass on both providers.

### Phase 5: Security Revalidation

**Tools:** `@sechek.security-scanner`, `appmod-dotnet-cve-check`
**Output:** `docs/05-security-comparison.md`

1. Re-run sec-check on `modernized/` folder.
2. Re-run `appmod-dotnet-cve-check --project-path modernized/src/[AppName] --compare-to [legacyPath]`.
3. Produce before/after delta:
   - Finding count reduction
   - Severity breakdown comparison
   - Score improvement
   - Residual risk list

### Phase 6: Test Coverage and Build Validation

**Tools:** `appmod-dotnet-run-test`, `appmod-dotnet-build-project`
**No dedicated output doc** — test results are evidence in the modernized project

1. Build modernized project: `appmod-dotnet-build-project --project-path modernized/src/[AppName]`.
2. Run tests: `appmod-dotnet-run-test --project-path modernized/tests/[AppName].Tests --coverage`.
3. Validate:
   - Build succeeds with zero errors
   - All business-rule tests pass (especially extracted SP logic)
   - Coverage meets threshold (>80% on business logic)

### Phase 7: Architecture Documentation

**Tools:** `@rpi-agent`, Mermaid diagram generation
**Output:** `docs/07-architecture-documentation.md`

Generate:
- Before/after architecture comparison diagrams
- SP → C# migration flowchart
- Data flow sequence diagrams (sync ADO.NET → async EF Core)
- Dependency graph comparison
- Security posture comparison diagram

### Phase 8: Deployment Planning

**Tools:** `@pm-migration-agent`
**Output:** `docs/08-deployment-plan.md`

Generate:
- 4 deployment options with Mermaid architecture diagrams
- Decision matrix comparing options
- Migration Gantt chart
- Cost estimates per option
- Recommendation (start simple with App Service, evolve as needed)

---

## Output Contract

Running this skill produces exactly these result files in `docs/`:

| File | Phase | Content |
|---|---|---|
| `docs/01-legacy-assessment.md` | 1 | Combined HVE + AppMod + awesome-copilot assessment |
| `docs/02-security-baseline.md` | 2 | Baseline security evidence |
| `docs/03-modernization-plan.md` | 3 | Plan merging awesome-copilot + upgrade tool + security fixes |
| `docs/04b-database-migration.md` | 4B | SQL Server → PostgreSQL migration evidence with SP mapping, Mermaid diagrams, verification methods |
| `docs/05-security-comparison.md` | 5 | Before/after security delta |
| `docs/07-architecture-documentation.md` | 7 | Architecture diagrams |
| `docs/08-deployment-plan.md` | 8 | Deployment recommendation |

No action/orchestration content goes in `docs/`. All orchestration lives in this skill, the agents, and the prompt.

---

## Quality Gates

| Gate | Criterion | Tool |
|---|---|---|
| Assessment complete | All SPs mapped, all deps analyzed | `appmod-dotnet-run-assessment` |
| Security baseline | Score established, all CVEs documented | `appmod-dotnet-cve-check` + sec-check |
| Plan complete | awesome-copilot + upgrade tool results merged | `@task-planner` |
| Build passes | Zero compile errors on `net10.0` | `appmod-dotnet-build-project` |
| DB migration documented | SP mapping, PL/pgSQL translations, dual provider config | `mssql_*` + `pgsql_*` tools |
| Tests pass | All business-rule tests green | `appmod-dotnet-run-test` |
| Security improved | No critical CVEs remain, score > 85 | `appmod-dotnet-cve-check` + sec-check |
| Docs complete | All 7 result docs generated | file check |

---

## Reusable Customer Prompt

> "Apply the `dotnet10-modernization-customer` skill to `[legacyPath]`. Detect source framework version automatically, run phases 1-8, target `net10.0`, and generate all result docs with security and build evidence. Use awesome-copilot patterns and the upgrade tool together, merging both into the modernization plan."