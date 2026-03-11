# App Modernization Demo Script — Live Customer Walkthrough

## Demo Overview

| Field | Value |
|---|---|
| **Title** | End-to-End .NET Application Modernization with GitHub Copilot Agents, Skills & Tools |
| **Duration** | 70–85 minutes |
| **Audience** | Technical leads, architects, engineering managers |
| **Tools** | VS Code, GitHub Copilot, HVE Core (RPI), AppMod-Dotnet (AppCAT + CVE + build + test), sec-check, awesome-copilot patterns, dotnet-upgrade tool, MSSQL Extension, PostgreSQL Extension, pgLoader, .NET 10 SDK |
| **Legacy App** | eShopModernizing (.NET Framework 4.7.2, ASP.NET MVC 5, EF6, Autofac, stored procedures) |
| **Target** | .NET 10 with ASP.NET Core, EF Core 10, built-in DI, Serilog, OpenTelemetry, dual DB support (SQL Server + PostgreSQL) |
| **Key Differentiator** | One-click reusable skill+agent+prompt drives every phase; results are auditable docs |

---

## Pre-Demo Checklist

- [ ] VS Code open with workspace `sbux-appmod-demo`
- [ ] HVE Core extension installed (`task-researcher`, `task-planner`, `rpi-agent` visible in agent list)
- [ ] AppMod-Dotnet tools available: `appmod-dotnet-install-appcat`, `appmod-dotnet-run-assessment`, `appmod-dotnet-build-project`, `appmod-dotnet-cve-check`, `appmod-dotnet-run-test`
- [ ] Custom agent visible: `.github/agents/appmodernization-dotnet.agent.md`
- [ ] Customer skill visible: `.github/skills/dotnet10-modernization-customer/SKILL.md`
- [ ] One-click replay prompt: `.github/prompts/dotnet10.modernize.agent-upgrades-v1.prompt.md`
- [ ] sec-check visible in workspace
- [ ] .NET 10 SDK installed (`dotnet --version` → 10.x)
- [ ] NuGet online source configured (`nuget.org`) for live build/test
- [ ] SQL Server LocalDB available (for live DB demos)
- [ ] MSSQL Extension installed in VS Code (for source DB inspection)
- [ ] PostgreSQL Extension installed in VS Code (for target DB validation)
- [ ] pgLoader installed (optional — for live data migration demo)
- [ ] Pre-generated backup docs in `docs/` folder (safety net)
- [ ] `versions/agent-upgrades-v1/` folder reset and ready (run `.\versions\agent-upgrades-v1\run-replay.ps1`)
- [ ] Clean VS Code theme, no distracting extensions

---

## Opening — Set the Scene (3 min)

### Narration:

> *"Today I'm going to show you a complete application modernization journey — legacy .NET Framework 4.7.2 with stored procedures, all the way to .NET 10. We'll use GitHub Copilot with specialized agents, a reusable skill, and dedicated assessment tools at every step."*

> *"What makes this different: I have ONE skill file, ONE agent, and ONE prompt. I press one button, and it runs eight phases — from assessment through deployment planning. Every phase produces an auditable evidence document. The orchestration lives in .github; the results live in a versioned output folder. Nothing is mixed."*

### Show the workspace:

1. **File Explorer** — show the top-level folders:
   - `legacy/` — the .NET Framework app we're modernizing (existing, no re-download)
   - `hve-core/` — RPI methodology framework
   - `sec-check/` — security scanner
   - `versions/agent-upgrades-v1/` — where this run's output goes (clean, empty)
   - `.github/` — agents, skills, prompts (all orchestration)

2. **Open the legacy app briefly:**
   - Navigate to `legacy/eShopLegacyMVCSolution/src/eShopLegacyMVC/`
   - Point out `Global.asax.cs`, `Web.config`, `packages.config`
   - *"This is what many enterprise apps look like — .NET Framework 4.7.2, Autofac DI, Entity Framework 6, log4net, and stored procedures handling business logic."*

3. **Show the three orchestration assets** (quick 30-second flyby):
   - `.github/skills/dotnet10-modernization-customer/SKILL.md` — *"Single source of truth for how to run the modernization."*
   - `.github/agents/appmodernization-dotnet.agent.md` — *"The agent that reads the skill and executes it."*
   - `.github/prompts/dotnet10.modernize.agent-upgrades-v1.prompt.md` — *"One click, eight phases."*

---

## Phase 0: One-Click Bootstrap (5 min)

### Narration:

> *"I'm going to kick off the entire modernization with a single prompt. It installs assessment tooling, detects the source framework version, and begins generating evidence docs."*

### Live Actions:

1. **Open Copilot Chat** (`Ctrl+Alt+I`)

2. **Run the one-click replay prompt:**
   ```
   /dotnet10.modernize.agent-upgrades-v1 legacy/eShopLegacyMVCSolution
   ```

3. **While it starts, narrate the flow:**
   > *"Behind the scenes, the agent reads the skill file, installs AppCAT if needed, then auto-detects the source .NET version from the project files — we never hardcode it. It targets net10.0 and runs phases 1 through 8."*

4. **Show AppCAT install confirmation:**
   > *"AppCAT 1.0.878 — the .NET compatibility assessment tool — just installed automatically."*

5. **Fallback:** If the prompt doesn't trigger, select the `appmodernization-dotnet` agent directly and paste the skill instructions manually.

### Key Demo Moment:
> *"This is reusable. Same prompt, same skill, same agent — works on any .NET Framework codebase, any starting version."*

---

## Phase 1: Legacy Assessment (10 min)

### Narration:

> *"Phase 1 combines three sources: HVE Core's task-researcher gives qualitative depth, AppCAT gives objective compatibility telemetry, and awesome-copilot provides upgrade pathway analysis. We merge all three."*

### Live Actions:

1. **Show the AppCAT assessment running** against the solution

2. **When it completes, show the key numbers:**

   | Metric | Value |
   |---|---|
   | Projects analyzed | 3 |
   | Total issues | 10 |
   | Total incidents | 26 |
   | Total effort (story points) | 76 |
   | Mandatory blockers | 2 |
   | Optional | 7 |
   | Potential | 17 |

3. **Walk through the top findings:**
   - **`Runtime.0002`** — legacy .NET Framework targets must be migrated for Linux/container hosts
   - **`Local.0004`** — file-based log4net appender is a portability and security concern
   - **`Local.0007`** — machine-name dependencies (`Environment.MachineName`) won't work in containers
   - **`Connection.0001`** — connection string patterns need externalization

4. **Open the generated doc:** `versions/agent-upgrades-v1/docs/01-legacy-assessment.md`
   - Show detected frameworks: `.NETFramework v4.7.2` (main), `.NETFramework v4.6.1` (utilities)
   - Show the technology inventory: ASP.NET MVC 5, EF 6.2, Autofac, log4net, stored procedures
   - *"Everything detected automatically — no manual inventory needed."*

5. **Call out the stored procedures:**
   - Open `legacy/eShopLegacyMVCSolution/src/eShopLegacyMVC/Models/Infrastructure/StoredProcedures.sql`
   - *"7 stored procedures. The business logic is HERE, not in the app. We need to extract this."*

### Key Demo Moment:
> *"10 issues, 76 story points of effort — and that's before we even look at security. This is the data-driven case for modernization."*

---

## Phase 2: Security Baseline (8 min)

### Narration:

> *"Before we invest in upgrading, we need the security baseline. This gives us a measurable 'before' number."*

### Live Actions:

1. **Show the NuGet CVE check results** (from `appmod-dotnet-cve-check`):
   - 13 packages scanned
   - **1 vulnerable: `Newtonsoft.Json 12.0.1`** — HIGH severity
   - Advisory: `GHSA-5crp-9r3c-p9vr` (DoS via nested JSON)
   - Affected versions: everything below 13.0.1

2. **Show the config security findings:**
   - Open `Web.config` — *"Connection string right here in plaintext: `Data Source=(localdb)\MSSQLLocalDB`"*
   - Open `Web.Release.config` — *"Same pattern in every environment config"*
   - Open `CatalogServiceSP.cs` line 34 — *"Direct connection string access from the EF context — classic legacy pattern"*

3. **Show log4net concern:**
   - Open `log4Net.xml` — *"File appender. AppCAT flagged this as mandatory to fix for any cloud target."*

4. **Open the generated doc:** `versions/agent-upgrades-v1/docs/02-security-baseline.md`

5. **Summarize the baseline risk:**
   - Dependency risk: **Elevated** (known high CVE in JSON stack)
   - Configuration risk: **Moderate** (connection strings in config files)
   - Operational risk: **Moderate** (file logging, machine-name assumptions)

### Key Demo Moment:
> *"One high-severity CVE in a package EVERY .NET developer uses. And connection strings in plaintext in source control. This is what we fix."*

---

## Phase 3: Modernization Plan (7 min)

### Narration:

> *"Phase 3 is where awesome-copilot and the dotnet-upgrade tool earn their keep. We merge proven migration patterns from both into one plan — not guesswork."*

### Live Actions:

1. **Open the generated plan:** `versions/agent-upgrades-v1/docs/03-modernization-plan.md`

2. **Walk through each step:**

   | Step | What Changes | Security Tie-In |
   |---|---|---|
   | 1. Project system | Legacy csproj → SDK-style, target `net10.0` | Removes obsolete build dependencies |
   | 2. Package migration | EF6→EF Core, Autofac→built-in DI, log4net→Serilog, Newtonsoft→System.Text.Json | Eliminates high-severity CVE |
   | 3. Startup/config | Global.asax→Program.cs, Web.config→appsettings.json+secrets | Eliminates plaintext connection strings |
   | 4. Data/business logic | SP→EF Core LINQ, ADO.NET→async EF, business rules→C# domain | Removes raw SQL injection risk |
   | 5. Validation | Build, test, security re-scan | Quality gates |

3. **Call out the merge:**
   > *"See 'Planning Inputs Merged' at the top — awesome-copilot patterns for package and startup recipes PLUS dotnet-upgrade recommendations for sequencing. Both fed into this plan. That's what the skill enforces."*

4. **Show the risk section:**
   - *"The plan identifies NuGet offline source as a risk and provides the mitigation. This is honest planning, not hope."*

### Key Demo Moment:
> *"Every step resolves a security finding. We don't fix security later — we fix it AS we upgrade."*

---

## Phase 4: Live Code Modernization (20 min)

### Narration:

> *"Now let's see the implementation. The agent used rpi-agent plus awesome-copilot templates to produce a complete .NET 10 application."*

### Live Actions — 4 Key Transformations:

#### 4A: Project File (3 min)

1. **Show legacy:** Open `legacy/eShopLegacyMVCSolution/src/eShopLegacyMVC/eShopLegacyMVC.csproj`
   - *"573 lines of XML. ToolsVersion 15, GAC references, HintPaths, conditional imports."*

2. **Show modern:** Open `versions/agent-upgrades-v1/modernized/src/eShopModernized/eShopModernized.csproj`
   - *"30 lines. SDK-style. 8 clean packages. net10.0."*

   ```xml
   <Project Sdk="Microsoft.NET.Sdk.Web">
     <PropertyGroup>
       <TargetFramework>net10.0</TargetFramework>
       <Nullable>enable</Nullable>
       <ImplicitUsings>enable</ImplicitUsings>
     </PropertyGroup>
   </Project>
   ```

#### 4B: Startup Migration (5 min)

1. **Show legacy:** Open `Global.asax.cs`
   - *"Autofac ContainerBuilder, RouteConfig, BundleConfig, FilterConfig — all the App_Start ceremony."*

2. **Show modern:** Open `Program.cs`
   - *"Everything in one file. Built-in DI, middleware pipeline, modern configuration."*
   - Point out: HTTPS redirect, security headers middleware, Serilog, health checks, Swagger

3. **Key security additions:**
   - `app.UseHttpsRedirection()` + `app.UseHsts()`
   - Custom security headers middleware
   - `UseSerilogRequestLogging()` with PII scrubbing
   - `/health` and `/ready` endpoints

#### 4C: Stored Procedure → C# Domain ⭐ Star of the Demo (7 min)

1. **Show legacy SQL:** Open `StoredProcedures.sql`
   - *"7 stored procedures. The critical one is sp_UpdateInventory — 4 business rules buried in T-SQL."*

2. **Show legacy C#:** Open `CatalogServiceSP.cs`
   - *"Manual ADO.NET: SqlCommand, AddWithValue, SqlDataReader mapping. Untestable without a database."*

3. **Show the modern replacements:**
   - `CatalogRepository.cs` — *"Same queries, but EF Core LINQ. Type-safe, async, parameterized."*
   - `CatalogItem.AdjustStock()` — *"The 4 business rules from sp_UpdateInventory are now a C# domain method. We can unit test this without infrastructure."*
   - `InventoryService.cs` — *"Clean separation: domain logic in the entity, persistence in the service."*

4. **Narration:**
   > *"This is the most valuable transformation. Business rules move from SQL where they're invisible to C# where they're testable, version-controlled, and visible to every developer on the team."*

#### 4D: New Capabilities (5 min)

1. **Show** `InventoryController.cs` — *"NEW REST API endpoint: POST /api/inventory/{id}/adjust. The SP was only accessible via direct SQL calls. Now it's an API."*
2. **Show** Swagger config — *"Automatic API documentation out of the box."*
3. **Show** health check endpoints — *"Container-ready: /health and /ready. Kubernetes and App Service use these."*

---

## Phase 4B: SQL Server → PostgreSQL Database Migration (10 min)

### Narration:

> *"Now let's tackle a question enterprise customers always ask: what about database migration? SQL Server is great, but many teams want PostgreSQL — open source, lower licensing cost, Azure Flexible Server with Entra ID auth. Let me show you the three-step path."*

### Live Actions:

#### Step 1: Show the T-SQL Stored Procedures (2 min)

1. **Open** `legacy/.../StoredProcedures.sql`
   - *"7 T-SQL stored procedures. The business logic is IN the database — variable declarations with @-prefix, BEGIN TRY/CATCH error handling, BIT flags, RAISERROR."*
   - Point out `sp_UpdateInventory`: *"This is the critical one — 4 business rules buried in T-SQL."*

#### Step 2: Show the PL/pgSQL Translation (3 min)

1. **Open** `legacy/.../StoredFunctions_PostgreSQL.sql`
   - *"Here's the PostgreSQL equivalent — PL/pgSQL functions. Every stored procedure translated, with inline comments showing what changed."*

2. **Walk through key differences (the 7 areas):**

   | T-SQL | PL/pgSQL | Example |
   |---|---|---|
   | `DECLARE @var TYPE` | `DECLARE var TYPE` | No @ prefix |
   | `BEGIN TRY...CATCH` | `BEGIN...EXCEPTION` | Different error blocks |
   | `#TempTable` | `CREATE TEMP TABLE` | Explicit creation |
   | `@@TRANCOUNT` | Savepoints | Different txn model |
   | `CHARINDEX` | `POSITION` | Different string funcs |
   | `GETDATE()` | `NOW()` | Different date funcs |
   | `IDENTITY(1,1)` | `GENERATED ALWAYS AS IDENTITY` | Different auto-increment |

3. **Call out SP7 split:**
   > *"sp_GetInventoryReport returned TWO result sets from one procedure. PostgreSQL functions return one result set per function — so it's split into get_inventory_report_by_brand() and get_inventory_reorder_items()."*

#### Step 3: Show the C# EF Core Final State (2 min)

1. **Open** `CatalogItem.AdjustStock()` and `InventoryService.cs`
   - *"This is where the business logic ENDS UP — in C#, testable, version-controlled. The stored procedures were a necessary intermediate step to validate the translation, but the final modernized code doesn't use ANY stored procedures."*

#### Step 4: Show Dual DB Provider Configuration (3 min)

1. **Open** `modernized/src/eShopModernized/appsettings.json`
   - *"One new key: DatabaseProvider. Set it to SqlServer or PostgreSQL. The app switches providers at startup."*

2. **Open** `modernized/src/eShopModernized/Program.cs`
   - *"Conditional provider registration — UseSqlServer or UseNpgsql based on config. Health checks switch too."*

3. **Open** `modernized/src/eShopModernized/appsettings.PostgreSQL.json`
   - *"Azure PostgreSQL Flexible Server connection — Entra ID passwordless auth. No password in the connection string."*

4. **Open** `docs/04b-database-migration.md`
   - *"Full evidence doc: labeled SP mapping table, 6 Mermaid diagrams, 19 syntax differences documented, 3 verification methods, pgLoader configuration."*

### Key Demo Moment:
> *"Three steps: T-SQL to PL/pgSQL to C# EF Core. The PL/pgSQL translation validates we understood the SQL correctly. The C# code makes it testable. And the app runs on BOTH SQL Server and PostgreSQL — switchable via config."*

---

## Phase 5: Security Re-Scan — After (5 min)

### Narration:

> *"Remember the baseline: connection strings in plaintext, a high-severity JSON CVE, file-based logging concerns. Let's see where we are now."*

### Live Actions:

1. **Show the modernized CVE check results:**
   - 11 packages scanned in the modernized stack
   - **0 vulnerabilities found**
   - *"Zero. Every package in the modern stack is clean."*

2. **Open the comparison doc:** `versions/agent-upgrades-v1/docs/05-security-comparison.md`

3. **Walk through the delta:**

   | Metric | Before | After | Delta |
   |---|---|---|---|
   | Packages with CVEs | 1 (HIGH) | 0 | **-1 (100% resolved)** |
   | Connection string exposure | 3 config files | Secret/env-var driven | **Eliminated** |
   | File-based logging | Mandatory fix | Serilog (structured, no PII) | **Eliminated** |
   | Raw SQL / ADO.NET | CatalogServiceSP | EF Core parameterized | **Eliminated** |

4. **Call out the full-stack reference numbers from prior run:**
   - *"The backup docs folder has the detailed 14→1 finding comparison from the full prior run: score 38→92, 93% finding reduction."*

### Key Demo Moment:
> *"Zero known CVEs in the modernized package set. That's the security story."*

---

## Phase 6: Build & Test Validation (5 min)

### Narration:

> *"Trust but verify. Every stored procedure business rule now has a unit test."*

### Live Actions:

1. **Show the test project:** `versions/agent-upgrades-v1/modernized/tests/eShopModernized.Tests/`

2. **Open** `CatalogItemInventoryTests.cs` — walk through test names:
   - `AdjustStock_SaleExceedsStock_ThrowsInvalidOperation` — *"Rule 1 from sp_UpdateInventory"*
   - `AdjustStock_StockDropsBelowThreshold_SetsOnReorderTrue` — *"Rule 3"*

3. **Run build and test** (requires `nuget.org` source configured):
   ```powershell
   cd versions\agent-upgrades-v1\modernized
   dotnet test eShopModernized.sln -c Release
   ```

4. **Show results:** 29 tests, all passing *(or show validation-summary.md if NuGet source is offline)*

5. **If NuGet offline source blocks restore:**
   - Open `versions/agent-upgrades-v1/logs/validation-summary.md`
   - *"The validation log honestly records what happened — NU1101 restore errors because this machine only has the VS Offline Packages source. Add nuget.org and it builds clean. The log is the audit trail."*
   - Quick fix:
     ```powershell
     dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
     dotnet test eShopModernized.sln -c Release
     ```

### Key Demo Moment:
> *"In the legacy app, you could NOT test sp_UpdateInventory without a database. Now you can test all 4 business rules with zero infrastructure."*

---

## Phase 7: Architecture Documentation (5 min)

### Narration:

> *"Phase 7 generates before-and-after architecture diagrams — automatically."*

### Live Actions:

1. **Open** `versions/agent-upgrades-v1/docs/07-architecture-documentation.md` in VS Code preview

2. **Walk through key diagrams:**
   - **Before/After architecture** — monolith with warning nodes vs. clean modern stack
   - **SP migration flowchart** — each SP → its C# replacement
   - **Data flow sequence diagrams** — synchronous ADO.NET → async EF Core
   - **Dependency graph** — 25+ packages with CVEs → 8 clean packages
   - **Security posture comparison** — red "before" → green "after"

3. **Key talking point:**
   > *"These are Mermaid diagrams — they live in markdown, they render in VS Code, GitHub, Azure DevOps. No Visio licenses needed."*

---

## Phase 8: Deployment Planning (5–10 min)

### Narration:

> *"The question every customer asks: how do we get this to production?"*

### Live Actions:

1. **Open** `versions/agent-upgrades-v1/docs/08-deployment-plan.md`

2. **Walk through the 4 options** with architecture diagrams:

   | Option | Best For | Monthly Cost |
   |---|---|---|
   | A: On-Prem IIS | Data locality requirements | Infrastructure-dependent |
   | B: Azure App Service | **Recommended start** — managed PaaS, lowest ops | ~$385/mo |
   | C: Container Apps | Scale-to-zero, container portability | ~$550/mo |
   | D: Microservices (AKS) | Full decomposition when ready | ~$889/mo |

3. **Show the decision matrix and Gantt chart:**
   - *"Phase 1: App Service in 3 weeks. Phase 2: Containerize. Phase 3: Microservices as needed."*

4. **Recommendation:**
   > *"Start with Option B. You can evolve to containers and microservices as your team and requirements grow. Don't over-engineer on day one."*

---

## Reusable Customer Handoff (3 min)

### Narration:

> *"Everything I just showed you is packaged as reusable assets your team can run on OTHER applications — not just this one."*

### Live Actions:

1. **Show the skill:** `.github/skills/dotnet10-modernization-customer/SKILL.md`
   - *"Single source of truth. 8 phases, quality gates, output contract — all documented."*
   - Show the trigger phrases: *"upgrade unknown .net app to .net 10"*

2. **Show the agent:** `.github/agents/appmodernization-dotnet.agent.md`
   - *"Reads the skill and has access to every tool we used today."*

3. **Show the prompt:** `.github/prompts/dotnet10.modernize.agent-upgrades-v1.prompt.md`
   - *"One click. Point it at a different legacy solution path and it runs the same 8 phases."*

4. **Show the output contract:**

   | Output | Path | Phase |
   |---|---|---|
   | Legacy assessment | `versions/[run]/docs/01-legacy-assessment.md` | 1 |
   | Security baseline | `versions/[run]/docs/02-security-baseline.md` | 2 |
   | Modernization plan | `versions/[run]/docs/03-modernization-plan.md` | 3 |
   | Modernized code | `versions/[run]/modernized/` | 4 |
   | **DB migration evidence** | **`versions/[run]/docs/04b-database-migration.md`** | **4B** |
   | Security comparison | `versions/[run]/docs/05-security-comparison.md` | 5 |
   | Validation log | `versions/[run]/logs/validation-summary.md` | 6 |
   | Architecture docs | `versions/[run]/docs/07-architecture-documentation.md` | 7 |
   | Deployment plan | `versions/[run]/docs/08-deployment-plan.md` | 8 |

5. **Key point:**
   > *"The orchestration is in .github. The results are in versions/. Clean separation. You can version your runs, compare them, and audit every decision."*

---

## Closing — Recap (3 min)

### Narration:

> *"Let me recap what we just did:"*

> 1. *"We detected a legacy .NET Framework 4.7.2 app with 7 stored procedures — 3 projects, 10 compatibility issues, 76 story points of effort identified by AppCAT."*

> 2. *"We ran a security baseline — found a high-severity CVE in Newtonsoft.Json, connection strings in plaintext, and file-based logging concerns."*

> 3. *"We created a modernization plan that merges awesome-copilot patterns AND dotnet-upgrade recommendations — security fixes baked into every step."*

> 4. *"We modernized to .NET 10 — stored procedures became C# domain methods, Autofac became built-in DI, EF6 became EF Core 10, and we added HTTPS, security headers, structured logging, health checks, and Swagger."*

> 5. *"We re-scanned — zero known CVEs in the modernized package set."*

> 6. *"We wrote 29 unit tests covering every stored procedure business rule — tests that were IMPOSSIBLE against the legacy app."*

> 7. *"We generated architecture diagrams and a deployment roadmap with 4 options and cost estimates."*

> 8. *"And we packaged it all as reusable assets: one skill, one agent, one prompt. Your team can run this on the next app tomorrow."*

> *"All orchestration in .github. All results in versions/. Every tool — HVE Core, AppMod-Dotnet, awesome-copilot, sec-check — combined into one repeatable workflow."*

---

## Backup / Recovery Plan

| Issue | Recovery |
|---|---|
| Copilot agent slow or unresponsive | Switch to pre-generated docs in `docs/` folder |
| AppCAT install fails | Show the JSON report already at `legacy/eShopLegacyMVCSolution/.github/appmod/dotnet-appcat/result/report.json` |
| sec-check scan hangs | Show `versions/agent-upgrades-v1/docs/02-security-baseline.md` |
| `dotnet build` fails (offline NuGet) | Show `versions/agent-upgrades-v1/logs/validation-summary.md` — explain the offline source blocker and add `nuget.org` live |
| `dotnet test` fails | Show test source files and the SP-to-test mapping |
| Mermaid diagrams don't render | Paste into [mermaid.live](https://mermaid.live) or show screenshots |
| HVE Core extension not installed | Use standard Copilot Chat with manual prompts |
| Prompt file not recognized | Select `appmodernization-dotnet` agent directly and paste skill instructions |

### Quick NuGet Fix (if needed live):
```powershell
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
cd versions\agent-upgrades-v1\modernized
dotnet test eShopModernized.sln -c Release
```

### Reset for a Fresh Run:
```powershell
.\versions\agent-upgrades-v1\run-replay.ps1
```

---

## Workspace Structure

```
sbux-appmod-demo/
├── legacy/                              ← Existing eShopModernizing repo (not re-downloaded)
│   └── eShopLegacyMVCSolution/
│       └── src/eShopLegacyMVC/
│           ├── Global.asax.cs                  ← Legacy entry point
│           ├── Web.config                      ← Connection strings in plaintext
│           ├── packages.config                 ← 30+ NuGet packages
│           ├── Models/Infrastructure/
│           │   ├── StoredProcedures.sql        ← 7 stored procedures
│           │   └── StoredFunctions_PostgreSQL.sql   ← PL/pgSQL translations (Step 2)
│           └── Services/
│               └── CatalogServiceSP.cs         ← ADO.NET SP-backed service
│
├── versions/
│   └── agent-upgrades-v1/              ← This run's isolated outputs
│       ├── docs/
│       │   ├── 01-legacy-assessment.md         ← Phase 1: AppCAT + HVE + awesome-copilot
│       │   ├── 02-security-baseline.md         ← Phase 2: CVE + config + code findings
│       │   ├── 03-modernization-plan.md        ← Phase 3: Merged awesome-copilot + upgrade plan
│       │   ├── 05-security-comparison.md       ← Phase 5: Before/after security delta
│       │   ├── 07-architecture-documentation.md ← Phase 7: Mermaid diagrams
│       │   └── 08-deployment-plan.md           ← Phase 8: 4 deployment options
│       ├── modernized/
│       │   ├── eShopModernized.sln
│       │   ├── src/eShopModernized/            ← .NET 10 application
│       │   └── tests/eShopModernized.Tests/    ← 29 unit tests
│       ├── logs/
│       │   ├── replay-start.txt
│       │   └── validation-summary.md           ← Phase 6: Build/test evidence
│       └── run-replay.ps1                      ← Reset script for clean re-runs
│
├── docs/                               ← Pre-generated backup (safety net)
│   ├── 01-legacy-assessment.md
│   ├── 02-security-baseline.md
│   ├── 03-modernization-plan.md
│   ├── 05-security-comparison.md
│   ├── 07-architecture-documentation.md
│   └── 08-deployment-plan.md
│
├── hve-core/                           ← Microsoft HVE Core RPI framework
├── sec-check/                          ← Security scanning toolkit
│
├── .github/                            ← All orchestration (agents, skills, prompts)
│   ├── agents/
│   │   ├── appmodernization-dotnet.agent.md    ← Orchestrator agent
│   │   └── pm-migration-agent.md               ← Phase 8 deployment agent
│   ├── skills/
│   │   ├── dotnet-modernization/
│   │   │   └── SKILL.md                        ← Internal reference skill
│   │   └── dotnet10-modernization-customer/
│   │       └── SKILL.md                        ← Customer reusable skill (single source of truth)
│   └── prompts/
│       ├── dotnet10.modernize.prompt.md        ← Original one-click prompt
│       └── dotnet10.modernize.agent-upgrades-v1.prompt.md  ← Versioned replay prompt
│
└── DEMO-SCRIPT.md                      ← This file
```

---

## Demo Timeline Summary

| Time | Phase | What Happens | Key Number |
|---|---|---|---|
| 0:00 | Opening | Set the scene, show workspace | — |
| 0:03 | Phase 0 | One-click bootstrap, AppCAT install | 1 prompt → 8 phases |
| 0:08 | Phase 1 | Legacy assessment | 10 issues, 76 story points |
| 0:18 | Phase 2 | Security baseline | 1 HIGH CVE (Newtonsoft.Json) |
| 0:26 | Phase 3 | Modernization plan | awesome-copilot + upgrade merged |
| 0:33 | Phase 4 | Live code walkthrough | 573 → 30 lines (csproj) |
| 0:53 | Phase 4B | SQL Server → PostgreSQL migration | 7 SPs → PL/pgSQL → C# EF Core |
| 1:03 | Phase 5 | Security re-scan | 0 CVEs in modern stack |
| 1:08 | Phase 6 | Build & test | 29 tests |
| 1:13 | Phase 7 | Architecture docs | Mermaid diagrams |
| 1:18 | Phase 8 | Deployment plan | 4 options, $385–$889/mo |
| 1:23 | Handoff | Reusable assets | 1 skill + 1 agent + 1 prompt |
| 1:26 | Close | Recap | — |
