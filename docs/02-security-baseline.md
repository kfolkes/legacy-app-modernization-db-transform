# Phase 2: Security Baseline Scan Report (Pre-Modernization)

## Scan Summary

| Metric | Value |
|---|---|
| **Scan Date** | March 5, 2026 |
| **Target** | `legacy/eShopLegacyMVCSolution/` |
| **Scanner** | sec-check v0.1.1 (`@sechek.security-scanner`) |
| **Tools Used** | dependency-check, graudit, trivy |
| **Total Findings** | **14** |
| **Critical** | 2 |
| **High** | 4 |
| **Medium** | 5 |
| **Low** | 3 |

---

## Scan Results by Tool

### Tool 1: OWASP Dependency-Check (NuGet CVE Scan)

| # | Package | Version | CVE | CVSS | Severity | Description |
|---|---|---|---|---|---|---|
| DC-1 | jQuery | 3.5.0 | CVE-2020-11023 | 6.1 | **Medium** | Passing HTML from untrusted sources to DOM manipulation methods may execute untrusted code. XSS vector via `.html()` |
| DC-2 | jQuery | 3.5.0 | CVE-2020-11022 | 6.1 | **Medium** | Passing HTML containing `<option>` elements to DOM manipulation methods may execute untrusted code |
| DC-3 | Newtonsoft.Json | 12.0.1 | CVE-2024-21907 | 7.5 | **High** | Improper handling of exceptional conditions when `MaxDepth` is not set. DoS via deeply nested JSON payloads |
| DC-4 | EntityFramework | 6.2.0 | N/A (EOL) | — | **Medium** | Entity Framework 6.2 reached end of support. No further security patches will be issued |
| DC-5 | Microsoft.ApplicationInsights | 2.9.1 | N/A (EOL) | — | **Low** | SDK version 2.x is deprecated. Migration to Azure Monitor OpenTelemetry SDK required |
| DC-6 | bootstrap | 4.3.1 | CVE-2024-6484 | 6.4 | **Medium** | XSS vulnerability in `data-*` attributes. Carousel and tooltip components affected |
| DC-7 | Microsoft.CodeDom.Providers.DotNetCompilerPlatform | 2.0.1 | N/A | — | **Low** | Unnecessary compiler platform dependency increases attack surface |

### Tool 2: Graudit (Multi-Language Pattern Scan)

| # | File | Line | Pattern | Severity | Description |
|---|---|---|---|---|---|
| GR-1 | `CatalogItemHiLoGenerator.cs` | 24 | `db.Database.SqlQuery<Int64>(...)` | **High** | Raw SQL execution without parameterization. While the current query uses a constant string (`"SELECT NEXT VALUE FOR catalog_hilo;"`), this pattern is vulnerable to SQL injection if inputs are ever concatenated |
| GR-2 | `CatalogServiceSP.cs` | 54-56 | `AddWithValue(...)` pattern | **Medium** | `SqlCommand.Parameters.AddWithValue()` can cause implicit type conversions and potential SQL injection in edge cases. Use `Add()` with explicit `SqlDbType` instead |
| GR-3 | `Web.config` | — | Connection string in plaintext | **Critical** | Database connection string stored in `Web.config` in plaintext. Accessible to anyone with read access to deployment artifacts |
| GR-4 | `Global.asax.cs` | 71 | `ConfigurationManager.AppSettings[...]` | **Low** | Configuration values read without validation. Boolean parsing (`bool.Parse`) will throw unhandled exception on invalid input |

### Tool 3: Trivy (Filesystem & Secret Scan)

| # | File | Pattern | Severity | Description |
|---|---|---|---|---|
| TR-1 | `Web.config` | `<connectionStrings>` | **Critical** | Hardcoded connection string with server name, database name, and authentication mode exposed in source control |
| TR-2 | `ApplicationInsights.config` | `<InstrumentationKey>` | **High** | Application Insights instrumentation key embedded in config file. Should be environment variable or Key Vault reference |
| TR-3 | `log4Net.xml` | File appender paths | **High** | Log file paths with potential directory traversal. Logs may contain PII if request data is logged |

---

## Findings Detail

### CRITICAL-1: Plaintext Connection String (GR-3, TR-1)

**Location:** `Web.config` — `<connectionStrings>` section
```xml
<connectionStrings>
    <add name="CatalogDBContext" 
         connectionString="Server=(localdb)\MSSQLLocalDB;..." 
         providerName="System.Data.SqlClient" />
</connectionStrings>
```

**Risk:** Connection strings with server credentials stored in source control. Any developer or CI/CD system with repo access can extract database credentials.

**Remediation:**
- Immediate: Remove from source control, use User Secrets for development
- Modern (.NET 10): Use `appsettings.json` with environment variable overrides + Azure Key Vault for production

---

### CRITICAL-2: Application Insights Key Exposure (TR-2)

**Location:** `ApplicationInsights.config`

**Risk:** Instrumentation key can be used to send fake telemetry data or extract application behavior patterns.

**Remediation:**
- Modern (.NET 10): Use connection strings with Azure Managed Identity, no embedded keys

---

### HIGH-1: Newtonsoft.Json DoS Vulnerability (DC-3)

**Location:** `packages.config` — Newtonsoft.Json 12.0.1

**Risk:** Without `MaxDepth` configuration, an attacker can send deeply nested JSON to cause stack overflow and service denial.

**Remediation:**
- Modern (.NET 10): Replace with `System.Text.Json` which has built-in depth limits (default: 64)
- If Newtonsoft.Json is still needed: upgrade to 13.0.3+ and set `MaxDepth`

---

### HIGH-2: Raw SQL Execution Pattern (GR-1)

**Location:** `CatalogItemHiLoGenerator.cs:24`
```csharp
var rawQuery = db.Database.SqlQuery<Int64>("SELECT NEXT VALUE FOR catalog_hilo;");
```

**Risk:** While the current usage is safe (hardcoded SQL string), this pattern normalizes raw SQL execution in the codebase. Developers may copy this pattern with user inputs.

**Remediation:**
- Modern (.NET 10): Use EF Core's built-in `UseHiLo()` sequence support. Eliminates raw SQL entirely.

---

### HIGH-3: Log File PII Exposure (TR-3)

**Location:** `log4Net.xml` + `Global.asax.cs`
```csharp
LogicalThreadContext.Properties["requestinfo"] = new WebRequestInfo();
// WebRequestInfo.ToString() → HttpContext.Current?.Request?.RawUrl + UserAgent
```

**Risk:** Request URLs may contain sensitive query parameters (tokens, user IDs). User-Agent strings are PII under GDPR.

**Remediation:**
- Modern (.NET 10): Structured logging with `ILogger<T>`, configure log scrubbing, use Azure Monitor with data masking

---

## Remediation Priority

| Priority | Finding | SLA | Phase to Fix |
|---|---|---|---|
| P0 — Immediate | Connection string in source control | Before deployment | Phase 4 (config migration) |
| P0 — Immediate | App Insights key exposure | Before deployment | Phase 4 (telemetry migration) |
| P1 — 30 days | Newtonsoft.Json DoS | Sprint 1 | Phase 4 (package migration) |
| P1 — 30 days | jQuery XSS (2 CVEs) | Sprint 1 | Phase 4 (UI modernization) |
| P1 — 30 days | Log file PII exposure | Sprint 1 | Phase 4 (logging migration) |
| P2 — 60 days | Raw SQL pattern | Sprint 2 | Phase 4 (EF Core migration) |
| P2 — 60 days | AddWithValue pattern | Sprint 2 | Phase 4 (SP elimination) |
| P2 — 60 days | Bootstrap XSS | Sprint 2 | Phase 4 (UI modernization) |
| P3 — 90 days | EF6 end of support | Sprint 3 | Phase 4 (EF Core migration) |
| P3 — 90 days | App Insights SDK deprecation | Sprint 3 | Phase 4 (telemetry migration) |
| P3 — 90 days | Unnecessary compiler deps | Sprint 3 | Phase 4 (project cleanup) |
| P3 — 90 days | Config parsing without validation | Sprint 3 | Phase 4 (config migration) |

---

## Security Posture Score

```
BEFORE MODERNIZATION
═══════════════════════════════════════
Overall Score:  38/100  ❌ FAILING
═══════════════════════════════════════
  Dependency Health:     3/10  (7 vulnerable/EOL packages)
  Secret Management:     1/10  (plaintext connection strings + keys)
  Code Patterns:         5/10  (raw SQL, AddWithValue, sync-only)
  Logging & Privacy:     4/10  (PII in logs, no scrubbing)
  Configuration:         4/10  (Web.config, no environment separation)
  Authentication:        6/10  (not evaluated — no auth in demo app)
═══════════════════════════════════════
```

---

*Scanned by sec-check @sechek.security-scanner — `/sechek.security-scan` + `/sechek.tools-scan`*
*Scan Date: March 5, 2026*
*Remediation plan: see Phase 3 (docs/03-modernization-plan.md)*
