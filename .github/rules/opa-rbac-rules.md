# RBAC + OPA Authorization Rules

> Reference: [Microsoft Architecture Center — Zero trust architecture](https://learn.microsoft.com/en-us/azure/architecture/guide/security/conditional-access-architecture)  
> Reference: [Azure RBAC overview](https://learn.microsoft.com/en-us/azure/role-based-access-control/overview)

## Authorization Layers

Authorization operates at three layers. All three are required for a complete security posture:

| Layer | Technology | Scope | Example |
|---|---|---|---|
| **Infrastructure RBAC** | Azure RBAC (Entra ID) | Who can deploy, configure, access Azure resources | "DevOps team can push to ACR; App Service has Managed Identity to access Key Vault" |
| **Application RBAC** | ASP.NET Core Authorization | Who can access which API endpoints | "Admin role can POST /api/catalog; Reader role can only GET" |
| **Fine-Grained AuthZ** | OPA (Open Policy Agent) | Who can access which specific records | "Catalog admin for Brand 'Contoso' can only edit Contoso products" |

## Layer 1: Infrastructure RBAC (Azure)

- Use **Azure Managed Identity** for all service-to-service authentication. No passwords, no connection strings with credentials.
- Assign **least-privilege Azure roles**:
  - App Service / Container App → `Azure Service Bus Data Sender` for publishing events
  - App Service / Container App → `Storage Blob Data Reader` for accessing images
  - App Service / Container App → `Azure Database for PostgreSQL Flexible Server Entra Administrator` for DB access
- Use **Azure Policy** to enforce compliance (e.g., "all storage accounts must have HTTPS-only enabled").

## Layer 2: Application RBAC (ASP.NET Core)

### Role Definitions

| Role | Permissions | Maps To |
|---|---|---|
| `CatalogAdmin` | Full CRUD on catalog items, brands, types | Entra ID security group `sg-catalog-admins` |
| `InventoryManager` | Adjust stock, view inventory reports | Entra ID security group `sg-inventory-managers` |
| `Reader` | Read-only access to catalog and inventory | Entra ID security group `sg-readers` |
| `SystemService` | Service-to-service calls (no user context) | Managed Identity of calling service |

### ASP.NET Core Authorization Setup

```csharp
// Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("CatalogAdmin", policy => policy.RequireRole("CatalogAdmin"))
    .AddPolicy("InventoryManager", policy => policy.RequireRole("InventoryManager"))
    .AddPolicy("ReaderOrAbove", policy => policy.RequireRole("Reader", "CatalogAdmin", "InventoryManager"))
    .AddPolicy("OpaAuthorized", policy => policy.AddRequirements(new OpaAuthorizationRequirement()));
```

### Controller Usage

```csharp
[ApiController]
[Route("api/[controller]")]
public class CatalogController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "ReaderOrAbove")]
    public async Task<IActionResult> GetCatalogItems() { ... }

    [HttpPost]
    [Authorize(Policy = "CatalogAdmin")]
    public async Task<IActionResult> CreateCatalogItem([FromBody] CreateCatalogRequest request) { ... }

    [HttpPut("{id}")]
    [Authorize(Policy = "OpaAuthorized")]  // Fine-grained: OPA checks brand ownership
    public async Task<IActionResult> UpdateCatalogItem(int id, [FromBody] UpdateCatalogRequest request) { ... }
}
```

## Layer 3: Fine-Grained Authorization (OPA)

### Architecture

```
Client Request
    → API Gateway / BFF
    → ASP.NET Core Authentication (JWT validation)
    → ASP.NET Core Authorization (role check)
    → OPA Sidecar (fine-grained policy check)
    → Controller Action
```

### OPA Sidecar Deployment

- OPA runs as a **sidecar container** alongside each microservice (same pod/container group).
- Policies are loaded from a shared policy bundle (OCI artifact in ACR or Git-based policy repo).
- OPA exposes a local REST API at `http://localhost:8181/v1/data/authz/allow`.
- Decision latency: < 1ms (policies evaluated in-memory).

### Rego Policy Example

```rego
package authz

import rego.v1

default allow := false

# Rule: CatalogAdmins can edit any product
allow if {
    input.method == "PUT"
    input.path[0] == "api"
    input.path[1] == "catalog"
    "CatalogAdmin" in input.roles
}

# Rule: Brand-scoped admins can only edit their own brand's products
allow if {
    input.method == "PUT"
    input.path[0] == "api"
    input.path[1] == "catalog"
    "BrandAdmin" in input.roles
    input.resource.brandId == input.user.assignedBrandId
}

# Rule: Everyone with Reader role can GET
allow if {
    input.method == "GET"
    "Reader" in input.roles
}

# Rule: InventoryManagers can adjust stock
allow if {
    input.method == "POST"
    input.path[0] == "api"
    input.path[1] == "inventory"
    contains(input.path[2], "adjust")
    "InventoryManager" in input.roles
}
```

### ASP.NET Core OPA Integration

```csharp
// OpaAuthorizationHandler.cs
public class OpaAuthorizationHandler : AuthorizationHandler<OpaAuthorizationRequirement>
{
    private readonly HttpClient _opaClient;

    public OpaAuthorizationHandler(IHttpClientFactory httpClientFactory)
    {
        _opaClient = httpClientFactory.CreateClient("OPA");
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OpaAuthorizationRequirement requirement)
    {
        var httpContext = context.Resource as HttpContext ?? throw new InvalidOperationException();

        var opaInput = new
        {
            method = httpContext.Request.Method,
            path = httpContext.Request.Path.Value?.Split('/').Where(s => !string.IsNullOrEmpty(s)),
            roles = context.User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value),
            user = new { sub = context.User.FindFirst("sub")?.Value },
        };

        var response = await _opaClient.PostAsJsonAsync("/v1/data/authz/allow", new { input = opaInput });
        var result = await response.Content.ReadFromJsonAsync<OpaResponse>();

        if (result?.Result == true)
            context.Succeed(requirement);
    }
}

// Register in DI
builder.Services.AddHttpClient("OPA", c => c.BaseAddress = new Uri("http://localhost:8181"));
builder.Services.AddSingleton<IAuthorizationHandler, OpaAuthorizationHandler>();
```

## Policy Management

- **Policy-as-code**: All Rego policies stored in Git (`.github/policies/` or dedicated policy repo).
- **Policy testing**: Use `opa test` to validate policies against test cases before deployment.
- **Policy bundles**: Package policies as OCI bundles, push to ACR, OPA sidecars pull on startup.
- **Audit trail**: OPA decision logs exported to Azure Monitor for compliance auditing.

## Integration with BFFs

- **Kotlin BFF**: Calls OPA locally (sidecar) before forwarding to downstream services. Uses `ktor-client` to call `http://localhost:8181`.
- **React BFF (Next.js)**: API routes call OPA before proxying. Uses `fetch('http://localhost:8181/v1/data/authz/allow')`.
- BFFs enforce coarse-grained roles; downstream services enforce fine-grained (resource-level) policies via OPA.

## Testing Authorization

```csharp
// Integration test: verify OPA denies cross-brand edit
[Fact]
public async Task UpdateCatalogItem_WrongBrand_Returns403()
{
    var client = _factory.CreateClient()
        .WithBearerToken(brandAdminToken: "contoso");

    var response = await client.PutAsJsonAsync(
        "/api/catalog/42",
        new { Name = "Hacked", BrandId = 99 }); // Brand 99 != Contoso

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}
```

## Anti-Patterns

- **Fat tokens**: Don't embed fine-grained permissions in JWT claims — use OPA for dynamic decisions.
- **Hardcoded roles**: Don't check roles in controller code (`if (User.IsInRole("Admin"))`) — use policies.
- **Permission explosion**: Don't create hundreds of Azure RBAC custom roles — use OPA for resource-level authz.
- **Skipping OPA in dev**: Always run OPA sidecar locally — mismatches between dev and prod policies cause security gaps.
