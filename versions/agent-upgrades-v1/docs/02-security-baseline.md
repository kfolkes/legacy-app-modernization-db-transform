# Phase 2: Security Baseline (Replay: agent-upgrades-v1)

## Run Context
- Date: March 5, 2026
- Source: `legacy/eShopLegacyMVCSolution`
- Inputs: AppCAT report + NuGet CVE check + code/config review

## CVE Baseline (NuGet)
Packages checked: 13

### Vulnerable package(s)
- `Newtonsoft.Json` — **HIGH** advisory `GHSA-5crp-9r3c-p9vr`
  - Affected versions: `< 13.0.1`
  - Legacy version in project: `12.0.1`

### Packages checked with no known CVEs
`Autofac`, `autofac.webapi2`, `EntityFramework`, `Microsoft.AspNet.WebApi*`, `Pipelines.Sockets.Unofficial`, `System.Buffers`, `System.Diagnostics.DiagnosticSource`, `System.Memory`, `System.Runtime.CompilerServices.Unsafe`.

## Code/Config Security Observations
- Connection strings present in `Web.config`, `Web.Debug.config`, `Web.Release.config`.
- Legacy project uses direct connection string access in `Services/CatalogServiceSP.cs`.
- AppCAT indicates local/file logging pattern concerns (`log4Net.xml` appender).

## Baseline Risk Summary
- Dependency risk: Elevated (known high-severity CVE in JSON stack)
- Configuration risk: Moderate (connection-string handling in legacy config files)
- Operational risk: Moderate (file logging and local-environment assumptions)

## Required Remediation Inputs for Phase 3
1. Replace `Newtonsoft.Json` usage path with modern serialization defaults where possible.
2. Move secrets/connection configuration to environment or secret providers.
3. Replace legacy logging config with structured logging pipeline.
4. Remove/replace remaining legacy package and framework dependencies as part of `net10.0` migration.
