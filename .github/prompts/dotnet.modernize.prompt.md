---
name: dotnet.modernize
description: One-click .NET Framework to .NET 10 modernization. Drives the GitHub Copilot App Modernization for .NET extension and uses its tools (appmod-dotnet-install-appcat, appmod-dotnet-run-assessment, appmod-dotnet-cve-check, appmod-dotnet-build-project, appmod-dotnet-run-test) end-to-end.
agent: appmodernization-dotnet
argument-hint: "[legacyPath] e.g. legacy/dotnet-eshop/eShopLegacyMVCSolution OR leave blank for default sample"
tools: ['read/readFile', 'search/codebase', 'search/fileSearch', 'search/textSearch', 'search/listDirectory', 'todo', 'agent', 'execute', 'edit', 'search']
---

# One-Click .NET 10 Modernization

Read the skill file `.github/skills/dotnet-modernization-flow/SKILL.md` first — it is the single source of truth for orchestration and lists the exact extension tools to invoke at each phase.

## Mode Selection

If `legacyPath` matches a folder under `legacy/dotnet-eshop/` → **Demo mode** (bundled eShop sample).
Otherwise → **BYO mode** against the customer-provided .NET source.

Source path argument: `${input:legacyPath:legacy/dotnet-eshop/eShopLegacyMVCSolution}`

## Required Outcome

Execute phases 0–8 end-to-end via the **App Modernization for .NET extension**. Result docs go to `docs/dotnet/` only.

### Phase Workflow (extension-tool driven)

1. **Phase 0 — Precheck**
   - Detect source framework from `*.csproj` / `packages.config` / `global.json` / `*.sln`.
   - Run **`appmod-dotnet-install-appcat`** (idempotent).
   - Verify .NET 10 SDK available.

2. **Phase 1 — Assessment** → `docs/dotnet/01-legacy-assessment.md`
   - Invoke **`appmod-dotnet-run-assessment`** (AppCAT) on `legacyPath`.
   - Cross-validate with `@appmodernization-dotnet` agent + awesome-copilot upgrade analyzer.
   - Record baseline `appmod-dotnet-build-project` status.

3. **Phase 2 — Security baseline** → `docs/dotnet/02-security-baseline.md`
   - Invoke **`appmod-dotnet-cve-check`** for NuGet CVEs.
   - Run `sec-check` for SAST + secrets + deps.

4. **Phase 3 — Plan** → `docs/dotnet/03-modernization-plan.md`
   - Merge `@appmodernization-dotnet` recommendations + awesome-copilot patterns + dotnet-upgrade recipes.
   - Enumerate the Azure target the extension will recommend (Container Apps default).

5. **Phase 4 — Implementation**
   - Invoke `@appmodernization-dotnet` to execute the plan.
   - Apply standard patterns: SDK-style csproj, minimal hosting, built-in DI, Serilog, EF Core, async + `CancellationToken`, User Secrets.
   - Add `// SECURITY FIX:` / `// MIGRATION:` comments at every change.
   - Validate incrementally with **`appmod-dotnet-build-project`**.

6. **Phase 5 — Security delta** → `docs/dotnet/05-security-comparison.md`
   - Re-run **`appmod-dotnet-cve-check`** + sec-check; produce before/after table.

7. **Phase 6 — Build + Test**
   - **`appmod-dotnet-build-project`** on modernized solution → must succeed.
   - **`appmod-dotnet-run-test`** on modernized test projects → must pass.

8. **Phase 7 — Architecture** → `docs/dotnet/07-architecture-documentation.md`
   - Before/after Mermaid + migration map citing which extension tool produced each change.

9. **Phase 8 — Deploy** → `docs/dotnet/08-deployment-plan.md`
   - Use extension's deployment recommendation (default = Azure Container Apps).
   - Generate `azd` + Bicep outline.

## Guardrails

- Never assume source framework version — always detect.
- Never skip an extension tool — they form the customer-recommended modernization path.
- Always target `net10.0`.
- Only write evidence to `docs/dotnet/`. No orchestration in `docs/dotnet/`.

## Final Response Format

1. Detected source framework + app type
2. Phase 0–8 status with the **extension tool** invoked at each phase
3. Security delta (before → after)
4. Build/test status (`appmod-dotnet-build-project` + `appmod-dotnet-run-test`)
5. awesome-copilot patterns applied
6. Azure target + open risks + next manual step
