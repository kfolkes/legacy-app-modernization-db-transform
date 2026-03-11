```prompt
---
name: dotnet10.modernize.agent-upgrades-v1
description: Re-run phases 1-8 from scratch using existing local repos, writing outputs to versions/agent-upgrades-v1.
agent: appmodernization-dotnet
argument-hint: "[legacyPath] e.g. legacy/eShopLegacyMVCSolution"
tools: ['read/readFile', 'search/codebase', 'search/fileSearch', 'search/textSearch', 'search/listDirectory', 'todo', 'agent', 'execute', 'edit', 'search']
---

# .NET 10 Modernization Replay (Agent Upgrades v1)

Read `.github/skills/dotnet10-modernization-customer/SKILL.md` first.

Use existing local repositories only. Do not clone or re-download anything.

Target legacy source:
`${input:legacyPath:legacy/eShopLegacyMVCSolution}`

## Output Contract

Write all replay artifacts into:
- `versions/agent-upgrades-v1/docs/`
- `versions/agent-upgrades-v1/modernized/`
- `versions/agent-upgrades-v1/logs/`

## Phase Workflow (1-8)

1. Run precheck and install AppCAT if needed.
2. Generate legacy assessment in `versions/agent-upgrades-v1/docs/01-legacy-assessment.md`.
3. Generate security baseline in `versions/agent-upgrades-v1/docs/02-security-baseline.md`.
4. Build modernization plan in `versions/agent-upgrades-v1/docs/03-modernization-plan.md` by merging awesome-copilot + dotnet-upgrade inputs.
5. Implement modernization to `versions/agent-upgrades-v1/modernized/`.
6. Generate security comparison in `versions/agent-upgrades-v1/docs/05-security-comparison.md`.
7. Run build and tests; save summary to `versions/agent-upgrades-v1/logs/validation-summary.md`.
8. Generate architecture and deployment docs in:
   - `versions/agent-upgrades-v1/docs/07-architecture-documentation.md`
   - `versions/agent-upgrades-v1/docs/08-deployment-plan.md`

## Guardrails

- Never re-download repos.
- Always target `net10.0`.
- Always merge awesome-copilot patterns + dotnet-upgrade output in phase 3.
- Keep orchestration in `.github`; keep outputs in `versions/agent-upgrades-v1/` only.
```