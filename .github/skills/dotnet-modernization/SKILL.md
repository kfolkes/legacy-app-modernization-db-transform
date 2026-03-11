---
name: dotnet-modernization
description: End-to-end .NET application modernization from any .NET Framework version to .NET 10 using HVE Core RPI methodology, security scanning, and AppCAT tooling
version: 1.0.0
author: Microsoft ISE
maturity: stable
requires:
  agents:
    - task-researcher
    - task-planner
    - rpi-agent
    - sechek.security-scanner
  tools:
    - appmod-dotnet-install-appcat
    - appmod-dotnet-run-assessment
    - appmod-dotnet-build-project
    - appmod-dotnet-cve-check
    - appmod-dotnet-run-test
trigger_phrases:
  - "modernize .net application"
  - "upgrade dotnet framework"
  - "migrate to .net 10"
  - "net framework to .net core"
  - "assess .net migration"
  - "stored procedure to ef core"
---

# .NET Application Modernization Skill

This skill orchestrates the complete end-to-end modernization of .NET Framework applications (any version) to .NET 10 using the **HVE Core Research-Plan-Implement (RPI)** methodology combined with:

- **VS Code AppMod-Dotnet tools** for automated assessment and CVE checking
- **sec-check security scanner** for baseline and post-migration validation
- **HVE Core agents** for research, planning, and implementation
- **awesome-copilot patterns** for .NET upgrade best practices

## What This Skill Does

This skill automates the complete modernization lifecycle across **8 phases**:

1. **Phase 1: Legacy Assessment** — Deep analysis of existing codebase, dependencies, stored procedures, business logic
2. **Phase 2: Security Baseline** — Vulnerability scan before modernization
3. **Phase 3: Modernization Plan** — Integrated upgrade plan with security remediation
4. **Phase 4: Implementation** — Code transformation (project files, startup, EF Core, async patterns)
5. **Phase 5: Security Validation** — Re-scan to measure security improvements
6. **Phase 6: Test Coverage** — Unit tests for extracted business logic
7. **Phase 7: Architecture Documentation** — Before/after diagrams and technical guides
8. **Phase 8: Deployment Planning** — Cloud-ready deployment options with cost estimates

### Key Features

- **Version-agnostic**: Works with .NET Framework 1.1 through 4.8.x
- **Stored procedure extraction**: Converts T-SQL business logic to testable C# domain models
- **Security-integrated**: Scans before and after to measure concrete security improvements
- **Test-driven**: Generates unit tests proving business logic preservation
- **Cloud-ready**: Produces deployment plans for Azure App Service, Container Apps, or AKS
- **Fully documented**: Generates Mermaid architecture diagrams and migration guides

## When to Use This Skill

Use this skill when:

- You have a .NET Framework application (any version) that needs upgrading to .NET 10
- The application uses stored procedures with embedded business logic
- You need to measure and prove security improvements
- You want automated assessment before manual migration work
- You need deployment planning for Azure or containerized environments
- You're following the HVE Core RPI methodology

Do NOT use this skill for:

- Applications already on .NET Core 3.1+ (use `dotnet-upgrade` tool instead)
- Migrations to .NET versions older than .NET 10
- Non-.NET applications (Python, Java, Node.js)

---

## Phase 1: Legacy Assessment

### Objective
Establish verified truth about the legacy application before planning changes.

### Tools Used
- `@task-researcher` agent (HVE Core)
- `appmod-dotnet-run-assessment` (AppCAT CLI)
- File system analysis for manual inventory

### What Gets Produced
A comprehensive `01-legacy-assessment.md` document containing:

1. **Executive Summary**
   - Application type and .NET Framework version
   - Deployment model (IIS, Windows Service, console app)
   - Database backend and ORM strategy
   - Key business domains

2. **Code Inventory**
   - All controllers, services, repositories, models
   - Dependency injection container (Autofac, Unity, Ninject, built-in)
   - Logging framework (log4net, NLog, Serilog 2.x)
   - Configuration approach (Web.config, app.config, custom)

3. **Stored Procedure Analysis**
   - Complete inventory of all stored procedures
   - Business rules embedded in T-SQL
   - Migration map: each SP → proposed C# replacement
   - Complexity score per SP

4. **Dependency Analysis**
   - All NuGet packages with version numbers
   - Packages with known CVEs or EOL status
   - Packages with no .NET 10 equivalent
   - Recommended replacement packages

5. **AppCAT Report Integration**
   - Compatibility issues detected
   - API usage patterns that need rework
   - Platform-specific dependencies
   - Estimated effort score

### Prompt Template

```
@task-researcher Perform a comprehensive analysis of the .NET application in [PATH].

I need a complete inventory for .NET 10 modernization:

1. **Application Profile**:
   - Detect .NET Framework version from project files
   - Identify application type (ASP.NET MVC, Web Forms, Web API, WPF, WinForms, Console)
   - Database backend and connection string management

2. **Code Analysis**:
   - All controllers, services, repositories, models
   - Dependency injection container used
   - Logging and configuration frameworks
   - Async/await usage patterns

3. **Stored Procedure Extraction**:
   - List all stored procedures in [DB_SCRIPTS_PATH]
   - Extract business rules from T-SQL (return prevention, thresholds, state transitions)
   - Map each SP to its consuming C# code
   - Identify SPs that can be replaced with LINQ vs. those needing domain models

4. **Dependency Inventory**:
   - Parse packages.config or .csproj for all NuGet dependencies
   - Flag packages with CVEs
   - Flag packages with no .NET 10 equivalent
   - Suggest modern replacements

5. **Risk Assessment**:
   - Breaking changes from .NET Framework → .NET 10
   - Third-party dependencies locked to .NET Framework
   - Platform-specific APIs (System.Web, Windows identity, registry access)

Output format: Structured markdown with tables and code samples.
```

After research completes, run AppCAT:

```
appmod-dotnet-run-assessment --project-path [PATH] --output-format markdown
```

Merge both outputs into the final assessment document.

### Success Criteria
- ✅ All controllers and services inventoried
- ✅ Every stored procedure mapped to C# equivalent
- ✅ All NuGet dependencies analyzed with upgrade path
- ✅ AppCAT compatibility report generated
- ✅ Risk factors identified and scored

---

## Phase 2: Security Baseline

### Objective
Establish security posture BEFORE modernization to measure improvement.

### Tools Used
- `@sechek.security-scanner` agent
- `appmod-dotnet-cve-check` (NuGet vulnerability scanning)
- sec-check scanners: dependency-check, graudit, trivy

### What Gets Produced
A `02-security-baseline.md` report containing:

1. **Executive Summary**
   - Total findings count
   - Breakdown by severity (Critical, High, Medium, Low)
   - Overall security score (0-100)

2. **Vulnerability Details**
   - CVE numbers for affected packages
   - CVSS scores and exploitability ratings
   - Attack vector descriptions
   - Affected versions vs. fixed versions

3. **Code Security Issues**
   - Plaintext credentials in config files
   - SQL injection patterns (raw SqlCommand with string concatenation)
   - Logging PII exposure (passwords, emails, SSNs in logs)
   - Missing input validation
   - Weak cryptography usage

4. **NuGet Package CVEs**
   - All dependencies with known vulnerabilities
   - Transitive dependency CVEs
   - Remediation: upgrade version or replace package

5. **Risk Prioritization**
   - Critical: Must fix before deployment
   - High: Fix during modernization
   - Medium: Address in post-migration hardening
   - Low: Informational

### Prompt Template

```
@sechek.security-scanner Perform a deep security analysis of the .NET application in [PATH].

Focus areas:
1. **NuGet Package CVEs**: Scan packages.config and .csproj for all known vulnerabilities
2. **Connection String Security**: Check Web.config, app.config, and environment variables
3. **SQL Injection Patterns**: Find raw SqlCommand usage with string interpolation
4. **Logging PII**: Detect log statements that may expose sensitive data
5. **Authentication/Authorization**: Check Authorize attributes, role checks, claims usage
6. **Cryptography**: Identify weak hash algorithms (MD5, SHA1) or ECB mode usage

Generate a baseline score (0-100) and detailed finding list with remediation guidance.
```

Also run NuGet CVE check:

```
appmod-dotnet-cve-check --project-path [PATH] --include-transitive
```

### Success Criteria
- ✅ All CVEs documented with CVSS scores
- ✅ Code-level security issues identified
- ✅ Baseline security score established
- ✅ Findings categorized by severity
- ✅ Remediation steps documented

---

## Phase 3: Modernization Plan

### Objective
Create a phased implementation plan that integrates security fixes into the upgrade path.

### Tools Used
- `@task-planner` agent (HVE Core)
- awesome-copilot .NET upgrade patterns
- Security baseline report from Phase 2

### What Gets Produced
A `03-modernization-plan.md` document containing:

1. **Migration Strategy**
   - In-place upgrade vs. side-by-side rewrite
   - Big-bang vs. phased rollout
   - Rollback plan

2. **Step-by-Step Implementation Plan**
   Each step includes:
   - What changes
   - Which security issues it resolves
   - Estimated effort (hours/days)
   - Dependencies on previous steps
   - Validation criteria

3. **Stored Procedure Migration Strategy**
   - Which SPs convert to LINQ queries
   - Which SPs extract to domain models
   - Which SPs remain as raw SQL (complex reporting)
   - Test strategy for each approach

4. **Security Remediation Integration**
   - Which steps resolve Critical findings
   - Which steps resolve High findings
   - Post-migration security hardening tasks

5. **Risk Mitigation**
   - Breaking changes and workarounds
   - Third-party dependency replacements
   - Data migration concerns
   - Performance testing plan

### Prompt Template

```
@task-planner Create a phased .NET 10 modernization plan for the application assessed in [ASSESSMENT_PATH].

Incorporate security findings from [SECURITY_BASELINE_PATH].

The plan must cover:

1. **Project File Conversion**
   - Convert .csproj to SDK-style
   - Replace packages.config with PackageReference
   - Update target framework to net10.0
   - **Security fix**: Remove packages with CVEs listed in [CVE_LIST]

2. **NuGet Package Modernization**
   - Replace Autofac → Microsoft.Extensions.DependencyInjection
   - Replace log4net → Serilog 9.x with structured logging
   - Replace Entity Framework 6.x → EF Core 10.x
   - **Security fix**: Upgrade vulnerable packages to patched versions

3. **Configuration Modernization**
   - Migrate Web.config → appsettings.json
   - Move secrets to User Secrets (dev) and Azure Key Vault (prod)
   - **Security fix**: Remove plaintext connection strings (Critical finding #1, #2)

4. **Startup Modernization**
   - Global.asax → Program.cs with minimal hosting
   - RouteConfig → endpoint routing
   - BundleConfig → webpack or Vite
   - **Security fix**: Add security headers middleware, HTTPS redirect

5. **Data Access Modernization**
   - DbContext from EF6 → EF Core 10
   - Replace stored procedures with:
     - LINQ queries (for simple CRUD)
     - Domain model methods (for business logic)
     - Raw SQL (for complex reporting)
   - **Security fix**: Eliminate parameterized SQL injection risks (High findings #3-6)

6. **Async/Await Refactoring**
   - Convert all I/O-bound operations to async
   - Update controller actions to return Task<IActionResult>
   - Update repository interfaces to async signatures

7. **Test Coverage**
   - Extract stored procedure business logic to testable C# methods
   - Write unit tests covering ALL business rules
   - Write integration tests for repository layer

8. **Security Re-Validation**
   - Re-run sec-check scan
   - Compare before/after scores
   - Verify all Critical and High findings resolved

For each step, list:
- Files changed
- Which security findings it resolves (by ID)
- Estimated hours
- Validation steps
```

### Success Criteria
- ✅ Clear step-by-step implementation sequence
- ✅ Every security finding mapped to remediation step
- ✅ Stored procedure migration strategy defined
- ✅ Test strategy documented
- ✅ Rollback plan included

---

## Phase 4: Implementation

### Objective
Execute the modernization plan, transforming code to .NET 10.

### Tools Used
- `@rpi-agent` (HVE Core)
- GitHub Copilot Chat with `/explain`, `/fix`, `/tests` commands
- `appmod-dotnet-build-project` for validation
- awesome-copilot .NET patterns

### What Gets Produced
A complete modernized application in a `modernized/` directory containing:

1. **Project Structure** (SDK-style)
   ```
   modernized/
   ├── src/[AppName]/
   │   ├── [AppName].csproj         ← SDK-style, net10.0
   │   ├── Program.cs               ← Minimal hosting
   │   ├── appsettings.json         ← No secrets
   │   ├── appsettings.Development.json
   │   ├── Models/
   │   ├── Data/
   │   │   └── AppDbContext.cs      ← EF Core 10
   │   ├── Repositories/
   │   ├── Services/
   │   └── Controllers/
   └── tests/[AppName].Tests/
       └── [AppName].Tests.csproj
   ```

2. **Key Transformations**

   **Configuration**:
   ```csharp
   // Before (Web.config)
   <connectionStrings>
     <add name="DefaultConnection" connectionString="Server=...;Password=secret" />
   </connectionStrings>

   // After (Program.cs with User Secrets)
   builder.Configuration.AddUserSecrets<Program>();
   builder.Services.AddDbContext<AppDbContext>(options =>
       options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
   ```

   **Dependency Injection**:
   ```csharp
   // Before (Global.asax with Autofac)
   var builder = new ContainerBuilder();
   builder.RegisterType<CatalogService>().As<ICatalogService>();
   var container = builder.Build();
   DependencyResolver.SetResolver(new AutofacDependencyResolver(container));

   // After (Program.cs)
   builder.Services.AddScoped<ICatalogService, CatalogService>();
   ```

   **Stored Procedure Extraction**:
   ```csharp
   // Before (ADO.NET in service layer)
   public void UpdateInventory(int id, int quantity) {
       var cmd = new SqlCommand("EXEC sp_UpdateInventory @Id, @Qty", conn);
       cmd.Parameters.AddWithValue("@Id", id);
       cmd.Parameters.AddWithValue("@Qty", quantity);
       cmd.ExecuteNonQuery();
   }

   // After (Domain model + EF Core)
   public class CatalogItem {
       public InventoryUpdateResult AdjustStock(int quantityChange) {
           // RULE 1: Prevent negative stock
           if (AvailableStock + quantityChange < 0)
               throw new InvalidOperationException("Insufficient stock");
           
           // RULE 2: Prevent exceeding max threshold
           if (AvailableStock + quantityChange > MaxStockThreshold)
               throw new InvalidOperationException("Exceeds max threshold");
           
           // RULE 3: Auto-manage OnReorder flag
           AvailableStock += quantityChange;
           if (AvailableStock < RestockThreshold)
               OnReorder = true;
           
           return new InventoryUpdateResult { 
               NewStock = AvailableStock, 
               OnReorder = OnReorder 
           };
       }
   }

   // Usage in service
   public async Task UpdateStockAsync(int id, int quantity) {
       var item = await _context.CatalogItems.FindAsync(id);
       item.AdjustStock(quantity);
       await _context.SaveChangesAsync();
   }
   ```

3. **Security Enhancements**

   **HTTPS Enforcement**:
   ```csharp
   if (app.Environment.IsProduction()) {
       app.UseHsts();
       app.UseHttpsRedirection();
   }
   ```

   **Security Headers Middleware**:
   ```csharp
   app.Use(async (context, next) => {
       context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
       context.Response.Headers.Add("X-Frame-Options", "DENY");
       context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
       await next();
   });
   ```

   **Structured Logging (No PII)**:
   ```csharp
   // Before (log4net)
   log.Info($"User {email} logged in from {ipAddress}");

   // After (Serilog with redaction)
   _logger.LogInformation("User logged in from {IPAddress}", 
       RedactIPAddress(ipAddress));
   ```

### Implementation Workflow

1. **Setup**: Create new `modernized/` directory structure
   ```bash
   dotnet new sln -n [AppName]Modernized
   dotnet new mvc -n [AppName] -o src/[AppName] -f net10.0
   dotnet new xunit -n [AppName].Tests -o tests/[AppName].Tests -f net10.0
   dotnet sln add src/[AppName] tests/[AppName].Tests
   ```

2. **Migrate Project Files**: Use `@rpi-agent` to convert packages.config to PackageReference

3. **Migrate Configuration**: Convert Web.config to appsettings.json, extract secrets

4. **Migrate Startup**: Combine Global.asax, RouteConfig, FilterConfig into Program.cs

5. **Migrate Data Access**: 
   - Convert DbContext from EF6 to EF Core
   - Extract stored procedure business logic to domain models
   - Create repository interfaces

6. **Add Security Middleware**: HTTPS, headers, logging, health checks

7. **Build and Validate**:
   ```bash
   appmod-dotnet-build-project --project-path modernized/src/[AppName]
   ```

### Success Criteria
- ✅ Application builds successfully on .NET 10
- ✅ All stored procedures replaced with C# equivalents
- ✅ No secrets in source code
- ✅ Security middleware implemented
- ✅ Health check endpoints added
- ✅ OpenTelemetry instrumentation enabled

---

## Phase 5: Security Validation

### Objective
Re-scan the modernized application and prove security improvements.

### Tools Used
- `@sechek.security-scanner`
- `appmod-dotnet-cve-check`
- Comparison script

### What Gets Produced
A `05-security-comparison.md` report containing:

1. **Side-by-Side Comparison**
   | Metric | Before | After | Change |
   |--------|--------|-------|--------|
   | Total Findings | 14 | 1 | -93% |
   | Critical | 2 | 0 | -100% |
   | High | 4 | 0 | -100% |
   | Medium | 5 | 1 | -80% |
   | Low | 3 | 0 | -100% |
   | Security Score | 38/100 | 92/100 | +142% |

2. **Resolved Findings**
   - List each baseline finding with resolution proof
   - Show before/after code snippets
   - Verify with re-scan output

3. **Remaining Findings**
   - Justification for any unresolved issues
   - Remediation plan and timeline

4. **New Capabilities**
   - Security headers
   - Structured logging
   - Secrets management
   - Health checks for monitoring

### Prompt Template

```
@sechek.security-scanner Re-scan the modernized application in [MODERNIZED_PATH].

Use the same scan parameters as the baseline scan.

After scanning, create a comparison report showing:
1. How many findings were resolved
2. Before/after code snippets proving resolution
3. Any new findings introduced (there should be none)
4. Updated security score

Reference the baseline report at [BASELINE_PATH] for comparison.
```

Also run:
```bash
appmod-dotnet-cve-check --project-path modernized/src/[AppName] --compare-to legacy/
```

### Success Criteria
- ✅ All Critical findings resolved
- ✅ All High findings resolved
- ✅ Security score improved by 50+ points
- ✅ No new vulnerabilities introduced
- ✅ Detailed before/after comparison documented

---

## Phase 6: Test Coverage

### Objective
Prove business logic preservation through comprehensive unit tests.

### Tools Used
- `appmod-dotnet-run-test`
- xUnit with FluentAssertions and Moq
- GitHub Copilot `/tests` command

### What Gets Produced
A complete test suite in `tests/[AppName].Tests/` containing:

1. **Domain Model Tests** — Unit tests for extracted stored procedure logic
   - One test per business rule
   - Zero database dependencies
   - Fast execution (<100ms per test)

   Example:
   ```csharp
   [Fact]
   public void AdjustStock_SaleExceedsStock_ThrowsInvalidOperation()
   {
       // Arrange: Item has 10 units
       var item = new CatalogItem { AvailableStock = 10 };

       // Act & Assert: Selling 11 units should throw
       var act = () => item.AdjustStock(-11);
       act.Should().Throw<InvalidOperationException>()
          .WithMessage("Insufficient stock");
   }
   ```

2. **Repository Tests** — Integration tests with EF Core InMemory provider
   - Verify CRUD operations match stored procedure behavior
   - Test async methods
   - Validate query correctness

3. **Service Tests** — End-to-end tests orchestrating domain + persistence
   - Test transaction boundaries
   - Verify business rule enforcement at service layer

4. **Test Mapping Documentation**
   - Table showing: Stored Procedure → C# Method → Test Coverage
   - Business rule extraction proof

### Prompt Template

```
Generate comprehensive unit tests for the domain models extracted from stored procedures.

For each stored procedure business rule in [ASSESSMENT_PATH], create:
1. A unit test validating the happy path
2. Unit tests for each edge case and validation rule
3. Tests proving error handling matches the original SP behavior

Use:
- xUnit as the test framework
- FluentAssertions for readable assertions
- Moq for mocking dependencies
- EF Core InMemory provider for repository tests

Example test structure:
```csharp
public class CatalogItemInventoryTests
{
    [Fact]
    public void AdjustStock_ValidRestock_UpdatesStockAndClearsOnReorder() { }
    
    [Fact]
    public void AdjustStock_SaleExceedsStock_ThrowsException() { }
    
    [Fact]
    public void AdjustStock_StockDropsBelowThreshold_SetsOnReorderTrue() { }
}
```

Generate test classes for all domain models with extracted business logic.
```

Run tests:
```bash
appmod-dotnet-run-test --project-path tests/[AppName].Tests --coverage
```

### Success Criteria
- ✅ Every stored procedure business rule has a unit test
- ✅ All tests pass
- ✅ Code coverage >80% for domain models and services
- ✅ Test execution time <5 seconds
- ✅ Test-to-SP mapping documented

---

## Phase 7: Architecture Documentation

### Objective
Create visual documentation showing the before/after transformation.

### Tools Used
- GitHub Copilot with Mermaid.js diagram generation
- VS Code Markdown preview

### What Gets Produced
A `07-architecture-documentation.md` document with embedded Mermaid diagrams:

1. **Before/After Architecture Comparison**
   ```mermaid
   graph TB
       subgraph "Before - .NET Framework 4.7.2"
           A[IIS] --> B[ASP.NET MVC]
           B --> C[Autofac DI]
           C --> D[EF6]
           C --> E[ADO.NET]
           E --> F[(Stored Procedures)]
           D --> F
       end
       
       subgraph "After - .NET 10"
           G[Kestrel] --> H[ASP.NET Core]
           H --> I[Built-in DI]
           I --> J[EF Core 10]
           J --> K[(Domain Models)]
       end
   ```

2. **Stored Procedure Migration Flowchart**
   - Visual map of each SP → C# replacement
   - Complexity indicators

3. **Data Flow Sequence Diagrams**
   - Before: Controller → Service → SqlCommand → SP
   - After: Controller → Service → Repository → Domain Model → DbContext

4. **Dependency Graph**
   - Before: 25+ packages with CVE indicators
   - After: 8 clean packages

5. **Security Posture Comparison**
   - Visual score: 38 → 92
   - Finding count reduction chart

6. **State Machine Diagrams**
   - Business logic state transitions (e.g., OnReorder flag automation)

### Prompt Template

```
Create comprehensive architecture documentation with Mermaid diagrams showing the .NET modernization transformation.

Include:

1. **System Architecture Comparison**: Before/after diagrams showing:
   - Web server (IIS → Kestrel)
   - Framework (ASP.NET → ASP.NET Core)
   - DI container (Autofac → built-in)
   - ORM (EF6 → EF Core 10)
   - Data access (ADO.NET + SPs → LINQ + domain models)

2. **Stored Procedure Migration Map**: Flowchart showing:
   - Each SP name
   - Its business purpose
   - Target C# implementation (LINQ query, domain method, or raw SQL)
   - Complexity score (1-5)

3. **Authentication/Authorization Flow**: If applicable, show changes in auth patterns

4. **Deployment Architecture**: 
   - Before: IIS on-prem
   - After: Azure App Service / Container Apps / AKS options

5. **Data Model**: Entity relationship diagrams for EF Core entities

6. **Security Improvements**: Visual comparison of baseline vs. validation scores

Use Mermaid syntax for all diagrams. Make them renderable in GitHub and VS Code.
```

### Success Criteria
- ✅ At least 6 Mermaid diagrams created
- ✅ Diagrams render correctly in VS Code and GitHub
- ✅ Clear visual Before/After story
- ✅ Stored procedure migration fully mapped
- ✅ Security improvements visually highlighted

---

## Phase 8: Deployment Planning

### Objective
Provide actionable deployment options with architecture diagrams and cost estimates.

### Tools Used
- awesome-copilot Azure deployment patterns
- GitHub Copilot for cost estimation

### What Gets Produced
A `08-deployment-plan.md` document containing:

1. **Deployment Options Overview**
   - Option A: On-Premises IIS
   - Option B: Azure App Service (PaaS) — **RECOMMENDED**
   - Option C: Azure Container Apps (serverless containers)
   - Option D: AKS with .NET Aspire (microservices)

2. **Architecture Diagrams for Each Option**
   ```mermaid
   graph LR
       subgraph "Option B: Azure App Service"
           A[Azure Front Door] --> B[App Service]
           B --> C[Azure SQL Database]
           B --> D[Azure Key Vault]
           B --> E[Application Insights]
       end
   ```

3. **Decision Matrix**
   | Criteria | On-Prem IIS | App Service | Container Apps | AKS |
   |----------|-------------|-------------|----------------|-----|
   | Ops Complexity | High | Low | Medium | High |
   | Scalability | Manual | Auto | Auto | Auto |
   | Cost (monthly) | Variable | $385 | $512 | $889 |
   | Skills Required | IIS, Windows | Azure basics | Containers | K8s |
   | Time to Deploy | 2 weeks | 3 days | 1 week | 4 weeks |

4. **Migration Phases**
   - **Phase 1** (Weeks 1-2): Deploy to App Service (PaaS), migrate database to Azure SQL
   - **Phase 2** (Weeks 3-4): Containerize application, deploy to Container Apps
   - **Phase 3** (Months 2-3): Decompose to microservices, deploy to AKS with .NET Aspire

5. **Cost Analysis**
   - Infrastructure costs per option
   - Development/migration costs
   - Ongoing operational costs
   - ROI timeline

6. **GitHub Actions CI/CD Pipeline**
   - Workflow YAML for build + test + deploy
   - Environment-specific configurations
   - Security scanning integration

7. **Rollback Strategy**
   - Blue/green deployment
   - Database migration rollback plan
   - Traffic switching approach

### Prompt Template

```
Create a deployment planning document for the modernized .NET 10 application.

Include:

1. **Four Deployment Options**:
   - Detailed architecture diagram for each
   - Pros/cons comparison table
   - Cost estimates (monthly)
   - Deployment complexity rating

2. **Recommended Path**: Start with Azure App Service, evolve to containers/microservices

3. **Phased Migration Timeline**:
   - Phase 1: Lift-and-shift to App Service (3 days)
   - Phase 2: Containerize (1 week)
   - Phase 3: Decompose to microservices (2-3 months)

4. **Infrastructure as Code**: Bicep or Terraform templates for each option

5. **CI/CD Pipeline**: GitHub Actions workflow covering:
   - Build and test
   - Security scanning (sec-check integration)
   - Deploy to dev/staging/prod
   - Automated rollback on failure

6. **Monitoring Strategy**:
   - Application Insights for telemetry
   - Azure Monitor for infrastructure
   - Log Analytics for security audit
   - Alerts for error rate, latency, availability

7. **Cost Breakdown**:
   - Compute costs
   - Database costs
   - Storage costs
   - Networking costs
   - Total monthly estimate per option

Use the Azure Pricing Calculator for accurate estimates.
```

### Success Criteria
- ✅ Four deployment options documented with diagrams
- ✅ Cost estimates provided for each option
- ✅ Phased migration timeline created
- ✅ CI/CD pipeline template provided
- ✅ Monitoring and alerting strategy defined

---

## Complete Workflow Example

Here's a full end-to-end invocation example for a typical .NET Framework 4.7.2 application:

### Step 1: Install AppCAT (once per machine)
```bash
appmod-dotnet-install-appcat
```

### Step 2: Run Phase 1 — Legacy Assessment
```
@task-researcher Analyze the .NET application in ./legacy/MyApp/src/

Create a comprehensive assessment covering:
- Application type and .NET Framework version
- All controllers, services, models
- All stored procedures in ./legacy/MyApp/database/StoredProcedures/
- NuGet dependencies from packages.config
- Recommended upgrade path to .NET 10

Save output to ./docs/01-legacy-assessment.md
```

After Copilot completes:
```bash
appmod-dotnet-run-assessment --project-path ./legacy/MyApp/src/MyApp.Web/ --output ./docs/appcat-report.md
```

Merge both reports into `01-legacy-assessment.md`.

### Step 3: Run Phase 2 — Security Baseline
```
@sechek.security-scanner Deep scan the ./legacy/MyApp/src/ application.

Focus on:
- NuGet package CVEs
- Connection string exposure in Web.config
- SQL injection patterns
- Logging PII

Generate a security score and save to ./docs/02-security-baseline.md
```

Also run:
```bash
appmod-dotnet-cve-check --project-path ./legacy/MyApp/src/MyApp.Web/ --include-transitive
```

### Step 4: Run Phase 3 — Create Modernization Plan
```
@task-planner Create a phased .NET 10 upgrade plan referencing:
- Assessment: ./docs/01-legacy-assessment.md
- Security baseline: ./docs/02-security-baseline.md

Each step must:
- Describe what changes
- List which security findings it resolves
- Include validation criteria

Save to ./docs/03-modernization-plan.md
```

### Step 5: Run Phase 4 — Implementation
```
@rpi-agent Execute the modernization plan in ./docs/03-modernization-plan.md

Create a new ./modernized/ directory with a .NET 10 application that:
1. Uses SDK-style project files
2. Replaces all stored procedures with C# domain models or LINQ queries
3. Uses built-in DI instead of Autofac
4. Uses EF Core 10 instead of EF6
5. Removes all secrets from source code (use User Secrets)
6. Adds security headers middleware
7. Includes health check endpoints

Verify the build succeeds:
```

After code generation:
```bash
appmod-dotnet-build-project --project-path ./modernized/src/MyApp/
```

### Step 6: Run Phase 5 — Security Re-Validation
```
@sechek.security-scanner Re-scan ./modernized/src/ and compare to baseline.

Create a comparison report in ./docs/05-security-comparison.md showing:
- How many findings were resolved
- Before/after code snippets
- New security score
```

Also:
```bash
appmod-dotnet-cve-check --project-path ./modernized/src/MyApp/ --compare-to ./legacy/MyApp/src/MyApp.Web/
```

### Step 7: Run Phase 6 — Test Coverage
```
Generate a complete test suite in ./modernized/tests/MyApp.Tests/ covering:
- All business rules extracted from stored procedures
- Repository layer integration tests
- Service layer orchestration tests

Use xUnit, FluentAssertions, and EF Core InMemory.
```

Run tests:
```bash
appmod-dotnet-run-test --project-path ./modernized/tests/MyApp.Tests/ --coverage
```

### Step 8: Run Phase 7 — Architecture Documentation
```
Create comprehensive architecture documentation with Mermaid diagrams in ./docs/07-architecture-documentation.md showing:
- Before/after system architecture
- Stored procedure migration map
- Data flow comparisons
- Security posture improvement
```

### Step 9: Run Phase 8 — Deployment Planning
```
Create a deployment planning document in ./docs/08-deployment-plan.md with:
- Four deployment options (IIS, App Service, Container Apps, AKS)
- Architecture diagrams for each
- Cost estimates
- Phased migration timeline
- CI/CD pipeline template
```

---

## Troubleshooting

### Issue: AppCAT not installed

**Solution**: Run `appmod-dotnet-install-appcat` before assessment phase.

### Issue: Security scan fails due to missing tools

**Solution**: Ensure sec-check tools are installed:
```bash
# Check available scanners
cd sec-check
python -m agentsec.cli --list-skills

# Install missing tools (example for Trivy)
choco install trivy  # Windows
brew install trivy   # macOS/Linux
```

### Issue: Build fails after migration

**Solution**: Run diagnostics:
```bash
appmod-dotnet-build-project --project-path ./modernized/src/MyApp/ --verbose
```

Common issues:
- Missing NuGet package references
- Incompatible API usage (System.Web references)
- Configuration syntax errors

### Issue: Tests fail after stored procedure extraction

**Solution**: 
1. Compare SP business logic with domain model logic line-by-line
2. Use SQL Server Profiler to capture SP execution with sample data
3. Run domain model method with same inputs
4. Compare outputs and adjust logic until they match

### Issue: Mermaid diagrams don't render

**Solution**: 
- Verify syntax at https://mermaid.live/
- Ensure VS Code has Markdown Preview Mermaid Support extension
- Check for special characters that need escaping

---

## Success Metrics

Track these metrics to measure modernization success:

| Metric | Target | How to Measure |
|--------|--------|----------------|
| Security Score Improvement | +50 points | Compare Phase 2 vs Phase 5 reports |
| CVE Reduction | 90%+ resolved | Count baseline CVEs vs. post-migration |
| Test Coverage | >80% | Run `appmod-dotnet-run-test --coverage` |
| Build Time Reduction | 30-50% faster | Compare legacy vs. modernized build times |
| Stored Procedures Eliminated | 100% | Count SPs in legacy vs. modernized |
| Code Quality Score | Improved | Use SonarQube or CodeQL analysis |
| Deployment Automation | <10 min deploy | CI/CD pipeline execution time |

---

## References

### Tools Documentation
- [HVE Core RPI Methodology](../../hve-core/docs/methodology/rpi.md)
- [sec-check Security Scanner](../../sec-check/README.md)
- [AppCAT .NET Migration Tool](https://learn.microsoft.com/azure/developer/appcat/)
- [awesome-copilot .NET Patterns](https://github.com/awesome-copilot/patterns)

### Microsoft Learn Resources
- [Migrate from .NET Framework to .NET 10](https://learn.microsoft.com/dotnet/core/porting/)
- [EF Core Migration Guide](https://learn.microsoft.com/ef/core/what-is-new/ef-core-10.0/breaking-changes)
- [ASP.NET Core Fundamentals](https://learn.microsoft.com/aspnet/core/fundamentals/)
- [Azure App Service Deployment](https://learn.microsoft.com/azure/app-service/)

### Example Projects
- [eShopModernizing](https://github.com/dotnet-architecture/eShopModernizing) — Reference modernization project
- [.NET Upgrade Assistant Samples](https://github.com/dotnet/upgrade-assistant)

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2026-03-05 | Initial release with full 8-phase workflow |

---

*🤖 Crafted with precision by ✨Copilot following brilliant human instruction, then carefully refined by our team of discerning human reviewers.*
