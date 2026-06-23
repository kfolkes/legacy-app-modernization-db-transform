---
name: dotnet-modernization-flow
description: One-click reusable .NET Framework to .NET 10 modernization flow that drives the GitHub Copilot App Modernization for .NET extension. Detects source version, runs AppCAT assessment via the extension tools (appmod-dotnet-*), modernizes to net10.0, validates security and tests, and recommends an Azure target.
version: 1.1.0
author: AppMod Team
maturity: stable
requires:
  agents:
    - appmodernization-dotnet
    - security-auditor
    - audit-reviewer
    - doc-auditor
    - security-audit-interpreter
    - sechek.security-scanner
  extensions:
    - ms-dotnettools.vscode-dotnet-modernize   # App Modernization for .NET (AppCAT + appmod-dotnet-* tools)
  tools:
    - appmod-dotnet-install-appcat
    - appmod-dotnet-run-assessment
    - appmod-dotnet-build-project
    - appmod-dotnet-cve-check
    - appmod-dotnet-run-test
trigger_phrases:
  - "modernize .net app"
  - "upgrade .net framework to .net 10"
  - "dotnet modernization flow"
  - "one click dotnet upgrade"
  - "net framework to net10"
  - "migrate dotnet app to azure"
---

# .NET Framework → .NET 10 Modernization Flow

Single source of truth for the .NET modernization pipeline. Mirrors the Java flow phase-for-phase. All orchestration lives here. `docs/dotnet/` only holds generated results.

This flow is **driven by the GitHub Copilot App Modernization for .NET extension**. Every assessment, build, CVE, and test step is delegated to that extension's tools (`appmod-dotnet-*`) so the in-product experience matches the customer-facing extension UX.

---

## When to Use

- Source is any .NET Framework version (3.5+ / 4.x).
- Target is `net10.0`.
- Need a repeatable, audit-ready process backed by Microsoft's App Modernization for .NET tooling.

## Operating Modes

### Mode A — Demo (bundled sample)
- Source: `legacy/dotnet-eshop/eShopLegacyMVCSolution/` (or WebForms/NTier variants).
- Target: `modernized/dotnet-eshop/`.

### Mode B — BYO Codebase
- Source: any `.csproj`/`.sln` tree pointed to via `legacyPath`.
- Target: `modernized/<inferred-name>/`.

---

## Inputs

| Input | Required | Default |
|---|---|---|
| `legacyPath` | Yes | `legacy/dotnet-eshop/eShopLegacyMVCSolution` |
| `targetFramework` | No | `net10.0` |
| `azureTarget` | No | `container-apps` |
| `outputDir` | No | `modernized/<auto>/` |

---

## Extension-driven phase map

Every phase calls one or more **App Modernization for .NET extension** tools.

| Phase | Extension tool invoked | Purpose |
|---|---|---|
| 0 Precheck | `appmod-dotnet-install-appcat` | Install / verify AppCAT |
| 1 Assessment | `appmod-dotnet-run-assessment` + doc-auditor | Run AppCAT and produce baseline architecture + documentation maturity evidence |
| 2 Security baseline | `appmod-dotnet-cve-check` + sec-check + security-auditor | NuGet CVE scan, SAST/secrets, exposure analysis, finding chains, and missing controls |
| 3 Plan synthesis | `@appmodernization-dotnet` (agent) + awesome-copilot | Synthesize plan from assessment + CVEs |
| 4 Implementation | `@appmodernization-dotnet` (agent) | Execute upgrade |
| 5 Security delta | `appmod-dotnet-cve-check` + sec-check + audit-reviewer (re-run) | Compare scanner deltas, broken attack chains, remaining risk, owners, and deploy gap |
| 6 Build | `appmod-dotnet-build-project` | Compile modernized solution |
| 6 Test | `appmod-dotnet-run-test` | Run modernized test projects |
| 7 Architecture | doc-auditor + agent summary | Mermaid + migration map + customer-ready documentation audit |
| 8 Deployment | `@appmodernization-dotnet` deployment recommendation | Container Apps / App Service / AKS |

---

## Core Philosophy — Iterate to Consensus

Every critical step is validated by 2–3 independent tools.

| Step | Tool 1 | Tool 2 | Tool 3 |
|---|---|---|---|
| Source version detection | csproj/packages.config parse | global.json | sln metadata |
| Assessment | `appmod-dotnet-run-assessment` (AppCAT) | `@appmodernization-dotnet` agent | awesome-copilot upgrade analyzer |
| Documentation baseline | doc-auditor | AppCAT inventory | README / architecture / runbook evidence |
| Security baseline | sec-check | `appmod-dotnet-cve-check` | security-auditor + CodeQL |
| Plan synthesis | awesome-copilot patterns | dotnet-upgrade recipes | extension agent recommendations |
| Implementation | `@appmodernization-dotnet` agent | awesome-copilot templates | dotnet-upgrade tool |
| Build validation | `appmod-dotnet-build-project` | `dotnet build` | csproj diff |
| Test validation | `appmod-dotnet-run-test` | `dotnet test` | behavior parity |
| Security revalidation | sec-check delta | `appmod-dotnet-cve-check` delta | CodeQL delta |
| Documentation readiness | doc-auditor | architecture docs | deployment + runbook evidence |
| Azure readiness | extension deployment recommendation | Bicep/azd what-if | Container Apps probe |

---

## Phase-by-Phase

### Phase 0 — Precheck
1. Detect source framework from `*.csproj`, `packages.config`, `global.json`, `*.sln`.
2. Run **`appmod-dotnet-install-appcat`** (idempotent) so AppCAT is on PATH.
3. Verify .NET 10 SDK available (`dotnet --list-sdks` includes a `10.x` line).
4. Confirm output dir is empty or new.

### Phase 1 — Legacy Assessment
**Output:** `docs/dotnet/01-legacy-assessment.md`
- Invoke **`appmod-dotnet-run-assessment`** against `legacyPath`.
- Cross-validate with `@appmodernization-dotnet` agent (app type + framework features) and awesome-copilot upgrade analyzer.
- Detect features: WebForms, MVC, WCF, Autofac, log4net, EF6, async/sync split, packages.config vs PackageReference.
- Record baseline build status with `appmod-dotnet-build-project`.
- Use `.github/skills/shared/01-baseline-architecture-template.md` as the evidence shape.
- Produce more than an inventory: capture baseline architecture across runtime, hosting, dependency map, data, identity, integrations, operations, and customer handoff readiness.
- Run a documentation baseline using `.github/skills/shared/documentation-audit-template.md`: README/onboarding, architecture, API, deployment, runbooks, security, testing, ADRs, and developer/user guides.
- Identify architecture risk signals that affect migration or customer adoption: tight coupling, unsupported components, hardcoded configuration, local-only assumptions, hidden deployment dependencies, missing tests, missing observability, and missing ownership.
- Recommend ADRs or specs for modernization decisions that are not obvious from the code, such as Azure target, identity model, data migration, logging/telemetry, or deployment strategy.

### Phase 2 — Security Baseline
**Output:** `docs/dotnet/02-security-baseline.md`
- Invoke **`appmod-dotnet-cve-check`** for NuGet CVEs.
- Run `sec-check` (SAST + secrets + deps).
- Use `.github/skills/shared/02-security-baseline-template.md` as the evidence shape.
- Combine scanner output with consequence-based interpretation from `security-auditor` and `security-audit-interpreter`; do not present isolated CVEs as the whole baseline.
- Classify executive risk mode as `COVER`, `TRIAGE`, or `ASSUME BREACH` based on exposure, data sensitivity, and finding density.
- Review security categories: authentication, authorization/access control, API security, input handling, backend security, crypto/secrets, database/data protection, third-party dependencies, secure logging/detection, infrastructure/container posture, UI security, and AI-specific risks when applicable.
- Document finding chains: entry point, weaknesses chained, business impact, required fixes, owner, and deploy gap.
- Document missing controls that scanners cannot fully prove: detection coverage, incident response wiring, secret rotation, escalation owner, and customer deployment timeline.
- Convert findings into customer action priorities: fix now, plan next, accept with compensating control, or needs named owner.

### Phase 3 — Modernization Plan
**Output:** `docs/dotnet/03-modernization-plan.md`
- Merge `@appmodernization-dotnet` recommendations + awesome-copilot patterns + dotnet-upgrade recipes.
- Map every Phase-2 finding to a fix step.
- Phase plan into atomic, verifiable steps.
- Plan must enumerate which Azure target the extension will recommend (Container Apps default).

### Phase 4 — Implementation
- Invoke **`@appmodernization-dotnet`** agent to execute the plan.
- Apply standard modernized patterns:
  - SDK-style `*.csproj` (replaces packages.config)
  - Minimal hosting `Program.cs` (replaces `Global.asax`)
  - Built-in DI (replaces Autofac)
  - Serilog (replaces log4net)
  - EF Core with HiLo (replaces EF6)
  - Async + `CancellationToken`
  - User Secrets (replaces `Web.config` connection strings)
- Add `// SECURITY FIX:` and `// MIGRATION:` comments at every change.
- Validate incrementally with **`appmod-dotnet-build-project`** after each major step.

### Phase 5 — Security Revalidation
**Output:** `docs/dotnet/05-security-comparison.md`
- Re-run **`appmod-dotnet-cve-check`** + sec-check.
- Use `.github/skills/shared/05-security-comparison-template.md` as the evidence shape.
- Produce scanner before/after delta. Critical/High targets: 0.
- Compare risk reduction, not only finding counts: show which finding chains were broken, which links remain, and whether the executive risk mode changed.
- Record remaining risks, accepted risks, compensating controls, named owners, target dates, and release recommendation.
- Capture deploy gap explicitly: source fix, build artifact, test/staging deployment, all-customer deployment, and uncovered environments.

### Phase 6 — Build + Test Validation
- **`appmod-dotnet-build-project`** on modernized solution → must succeed.
- **`appmod-dotnet-run-test`** on modernized test projects → must pass.
- Behavior parity check: same inputs → same outputs across legacy and modernized.

### Phase 7 — Architecture Documentation
**Output:** `docs/dotnet/07-architecture-documentation.md`
- Before/after Mermaid diagrams.
- Migration map (legacy component → modernized replacement, citing the extension tool that produced each change).
- Re-run the documentation audit template and close or carry forward Phase-1 documentation gaps.
- Include runtime, data, identity, integration, operations, deployment, security, and rollback views where the app has those concerns.
- Record ADR recommendations for major modernization decisions and any customer-owned follow-up specs or runbooks.

### Phase 8 — Deployment Plan
**Output:** `docs/dotnet/08-deployment-plan.md`
- Use the extension's deployment recommendation (default = **Azure Container Apps** for modernized .NET 10 web apps; App Service for monoliths; AKS for multi-service).
- Generate `azd` + Bicep outline.
- Document MI permissions, probes, rollback strategy.

---

## Guardrails

- Never hardcode source framework version — always detect.
- Never skip an extension tool — they form the customer-recommended Azure modernization path.
- Always target `net10.0` unless `targetFramework` overridden.
- Only write evidence docs to `docs/dotnet/`. Never put orchestration content in `docs/dotnet/`.

---

## Final Response Format

1. Detected source framework + app type
2. Per-phase status (1–8) with the extension tool invoked at each phase
3. Security delta (before → after)
4. Build/test validation status (legacy reference build + modernized build via `appmod-dotnet-build-project`)
5. awesome-copilot patterns applied
6. Azure target + open risks + next manual step (e.g. `azd up`)
