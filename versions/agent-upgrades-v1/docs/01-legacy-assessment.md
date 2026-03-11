# Phase 1: Legacy Assessment (Replay: agent-upgrades-v1)

## Run Context
- Date: March 5, 2026
- Source path: `legacy/eShopLegacyMVCSolution`
- Source app type: ASP.NET MVC
- Detected source frameworks: `.NETFramework v4.7.2` (main app), `.NETFramework v4.6.1` (utilities)
- Assessment tool: AppCAT CLI 1.0.878.50543

## AppCAT Summary
- Total projects analyzed: 3
- Total issues: 10
- Total incidents: 26
- Total effort (story points): 76
- Severity chart: mandatory `2`, optional `7`, potential `17`

## Key Compatibility Findings
- `Runtime.0002`: legacy .NET Framework targets require migration for container/Linux targets.
- `Local.0004`: file-based log appender (`log4net`) is a portability/security concern.
- `Local.0007`: machine/environment-specific runtime assumptions detected.
- Scale/static-content findings indicate packaging and hosting model changes are needed.

## Technology Inventory (Detected)
- Framework: `.NET Framework 4.7.2`
- Web stack: ASP.NET MVC 5
- ORM: Entity Framework 6.2
- DI: Autofac
- Logging: log4net
- Data access: EF + stored procedure service (`CatalogServiceSP`)

## Migration Implications
- Move to SDK-style project and target `net10.0`.
- Replace Autofac with built-in DI registration.
- Replace EF6 with EF Core 10 patterns (including HiLo mapping).
- Replace Global.asax/App_Start with minimal-hosting `Program.cs`.
- Remove file-appender logging and move to structured logging.

## Output Handoff
This assessment feeds:
- Phase 2 security baseline (`02-security-baseline.md`)
- Phase 3 modernization plan (`03-modernization-plan.md`)
