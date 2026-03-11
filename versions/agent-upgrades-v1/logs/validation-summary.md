# Replay Validation Summary (agent-upgrades-v1)

## Run Context
- Date: March 5, 2026
- Modernized path: `versions/agent-upgrades-v1/modernized`
- Build tool: `appmod-dotnet-build-project`
- Test command: `dotnet test .\\eShopModernized.sln -c Release`

## Build Result
- Status: **Failed**
- Primary cause: package restore source limitation
- Error families:
  - `NU1101`: package not found in `Microsoft Visual Studio Offline Packages`
  - `NU1102`: `Microsoft.NET.Test.Sdk (>= 17.12.0)` unavailable in offline source

## Test Result
- Status: **Failed** (restore failure)
- Representative missing packages: `Microsoft.EntityFrameworkCore.SqlServer`, `Serilog.AspNetCore`, `xunit`, `Moq`, `Swashbuckle.AspNetCore`

## Security/CVE Validation
- Legacy package set: 1 vulnerable package (`Newtonsoft.Json`, high)
- Modernized package set: 0 known vulnerabilities in checked set

## Blocker to Clear
Configure an online NuGet feed (`https://api.nuget.org/v3/index.json`) for this environment and re-run build/test.
