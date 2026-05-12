# Java phase evidence

This folder contains the generated evidence documents from running `/java.modernize`. Files appear here in order as the agent completes each phase.

| File | Phase |
|---|---|
| `01-legacy-assessment.md` | Phase 1: Legacy Assessment |
| `02-security-baseline.md` | Phase 2: Security Baseline |
| `03-modernization-plan.md` | Phase 3: Modernization Plan |
| `05-security-comparison.md` | Phase 5: Security Revalidation (delta) |
| `07-architecture-documentation.md` | Phase 7: Architecture Documentation |
| `08-deployment-plan.md` | Phase 8: Azure Deployment Plan |

> Phase 4 (Implementation) writes source code to `../../modernized/java-asset-manager/`, not here.
> Phase 6 (Build + Test) appears as CI status, not a doc.

To regenerate: open Copilot Chat → `/java.modernize`.
