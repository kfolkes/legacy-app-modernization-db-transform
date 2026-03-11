# Phase 5: Security Comparison (Replay: agent-upgrades-v1)

## Comparison Scope
- Before: `legacy/eShopLegacyMVCSolution`
- After: `versions/agent-upgrades-v1/modernized`
- Evidence sources: AppCAT + NuGet CVE checks

## CVE Delta

### Legacy package set (13 checked)
- Vulnerable packages: **1**
- High-severity advisories: **1** (`Newtonsoft.Json` < 13.0.1)

### Modernized package set (11 checked)
- Vulnerable packages: **0**
- High/Critical advisories: **0**

## Security Improvements Observed
- Legacy JSON dependency vulnerability removed from target package plan.
- Modernized stack uses current ASP.NET Core and EF Core package families.
- Logging approach modernized away from legacy file appender config.
- Config migration plan removes legacy `Web.config` connection-string exposure pattern.

## Residual Risk
- Build/test in this environment is currently blocked by offline NuGet source configuration; this prevents full runtime security validation until package restore succeeds.

## Required Follow-up
1. Add `nuget.org` source in environment/CI image.
2. Re-run build + tests.
3. Re-run security scans post-successful restore/build to finalize replay attestation.
