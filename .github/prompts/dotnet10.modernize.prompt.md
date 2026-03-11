```prompt
---
name: dotnet10.modernize
description: One-click starter to run phases 1-8 of .NET modernization from unknown source version to .NET 10. Uses HVE Core + AppMod-Dotnet + awesome-copilot + dotnet-upgrade + sec-check.
agent: appmodernization-dotnet
argument-hint: "[legacyPath] e.g. legacy/eShopLegacyMVCSolution"
tools: ['read/readFile', 'search/codebase', 'search/fileSearch', 'search/textSearch', 'search/listDirectory', 'todo', 'agent', 'execute', 'edit', 'search']
---

# One-Click .NET 10 Modernization Starter

Read the skill file `.github/skills/dotnet10-modernization-customer/SKILL.md` first — it is the single source of truth for orchestration.

Run the full modernization accelerator on this path:

`${input:legacyPath:legacy/eShopLegacyMVCSolution}`

## Required Outcome

Execute phases 1-8 end-to-end. Produce result docs in `docs/` only.

### Phase Workflow

1. **Phase 0: Precheck**
   - Detect current .NET version from project metadata.
   - Install AppCAT if needed (`appmod-dotnet-install-appcat`).

2. **Phase 1: Legacy Assessment**
   - HVE researcher + AppMod assessment + awesome-copilot upgrade analysis.
   - Generate `docs/01-legacy-assessment.md`.

3. **Phase 2: Security Baseline**
   - sec-check + AppMod CVE check.
   - Generate `docs/02-security-baseline.md`.

4. **Phase 3: Modernization Plan**
   - Merge awesome-copilot patterns AND dotnet-upgrade tool results into plan.
   - Map security fixes per step.
   - Generate `docs/03-modernization-plan.md`.

5. **Phase 4: Implementation**
   - Apply plan using rpi-agent + awesome-copilot templates.
   - Validate build with `appmod-dotnet-build-project`.

6. **Phase 5: Security Revalidation**
   - Re-scan modernized output; produce before/after delta.
   - Generate `docs/05-security-comparison.md`.

7. **Phase 6: Test + Build Validation**
   - Run tests with `appmod-dotnet-run-test`.
   - Confirm build success and business-rule parity.

8. **Phase 7: Architecture Documentation**
   - Generate before/after diagrams and migration maps.
   - Save to `docs/07-architecture-documentation.md`.

9. **Phase 8: Deployment Plan**
   - Produce deployment options and rollout recommendation.
   - Save to `docs/08-deployment-plan.md`.

## Guardrails

- Never assume source framework version.
- Always target `net10.0`.
- Always merge awesome-copilot + upgrade tool results into the plan.
- Only write result evidence to `docs/`. No orchestration content in `docs/`.

## Final Response Format

1. Detected source framework and app type
2. Phase completion status (1-8)
3. Security baseline vs post-modernization delta
4. Build/test validation status
5. awesome-copilot patterns applied
6. Open risks and next action
```
