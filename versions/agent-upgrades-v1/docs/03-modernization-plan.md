# Phase 3: Modernization Plan (Replay: agent-upgrades-v1)

## Planning Inputs Merged
- Legacy assessment evidence (AppCAT + source inventory)
- Security baseline and CVE data
- **awesome-copilot .NET modernization patterns** (package and startup migration recipes)
- **dotnet-upgrade recommendations** (upgrade sequencing and compatibility workflow)

This plan explicitly merges awesome-copilot and dotnet-upgrade guidance into one implementation flow.

## Target
- Target framework: `net10.0`
- Target hosting model: ASP.NET Core minimal hosting
- Target output path: `versions/agent-upgrades-v1/modernized/`

## Stepwise Plan

### Step 1 — Project system modernization
- Convert legacy csproj to SDK-style format.
- Remove legacy build imports and framework-only compiler packages.
- Set `<TargetFramework>net10.0</TargetFramework>`.
- Security tie-in: reduces obsolete dependency surface.

### Step 2 — Package migration matrix
- `EntityFramework` -> `Microsoft.EntityFrameworkCore.*`
- `Autofac*` -> built-in `Microsoft.Extensions.DependencyInjection`
- `log4net` -> `Serilog.AspNetCore` (+ sinks as needed)
- `ApplicationInsights 2.x` -> `Azure.Monitor.OpenTelemetry.AspNetCore`
- `Newtonsoft.Json 12.0.1` -> modernized serialization strategy (and/or update beyond vulnerable range)
- Security tie-in: resolves known vulnerable/outdated packages.

### Step 3 — Startup/config migration
- `Global.asax` + `App_Start/*` -> consolidated `Program.cs`.
- `Web.config` connection strings -> secret/env-var driven config.
- Introduce health checks, HTTPS, and security headers.
- Security tie-in: eliminates config-secret exposure patterns.

### Step 4 — Data and business logic migration
- Preserve catalog business rules while moving EF6 patterns to EF Core 10.
- Replace stored-procedure service pathways with validated C#/EF flows where required.
- Maintain asynchronous data access with cancellation support.
- Security tie-in: removes legacy raw SQL/ADO-style risk paths.

### Step 5 — Validation and quality gates
- Build gate: solution must restore/build in `Release`.
- Test gate: unit/integration suite passes.
- Security gate: no known critical/high CVEs in final package set.
- Documentation gate: produce Phases 5/7/8 outputs.

## Risk and Mitigation
- **Risk:** Offline NuGet source blocks restore/build.
  - **Mitigation:** configure online package feed (`nuget.org`) in CI/dev image, then re-run build/test.
- **Risk:** Framework API breaks during startup migration.
  - **Mitigation:** staged compile checks per migration step and adapter shims where needed.

## Replay Execution Note
A modernized solution has been staged to `versions/agent-upgrades-v1/modernized/` for validation in this replay run.
