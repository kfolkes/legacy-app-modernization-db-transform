// =============================================================================
// .NET Aspire AppHost — eShop Orchestrator
// Follows: microservice-rules.md
// References: https://learn.microsoft.com/en-us/dotnet/aspire/
// =============================================================================

var builder = DistributedApplication.CreateBuilder(args);

// ── Infrastructure Resources ──

// PostgreSQL (modernized primary database)
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .WithDataVolume("eshop-postgres-data");

var catalogDb = postgres.AddDatabase("catalogdb");
var inventoryDb = postgres.AddDatabase("inventorydb");

// Redis (distributed caching for BFFs)
var redis = builder.AddRedis("redis")
    .WithRedisCommander()
    .WithDataVolume("eshop-redis-data");

// Azure Service Bus (command messaging)
var serviceBus = builder.AddAzureServiceBus("messaging")
    .AddQueue("inventory-commands")
    .AddQueue("catalog-commands")
    .AddTopic("domain-events", topic =>
    {
        topic.AddSubscription("inventory-handler");
        topic.AddSubscription("reporting-handler");
    });

// Azure Event Hubs (streaming telemetry + price changes)
var eventHubs = builder.AddAzureEventHubs("streaming")
    .AddEventHub("price-changes")
    .AddEventHub("stock-alerts")
    .AddEventHub("audit-events");

// Azure Application Insights (centralized observability)
var appInsights = builder.AddAzureApplicationInsights("appinsights");

// ── Microservices ──

// Catalog Service (.NET 10)
var catalogService = builder.AddProject<Projects.CatalogService>("catalog-service")
    .WithReference(catalogDb)
    .WithReference(serviceBus)
    .WithReference(eventHubs)
    .WithReference(appInsights)
    .WithEnvironment("DatabaseProvider", "PostgreSQL")
    .WithHttpEndpoint(port: 5001)
    .WithHttpsEndpoint(port: 5011);

// Inventory Service (.NET 10)
var inventoryService = builder.AddProject<Projects.InventoryService>("inventory-service")
    .WithReference(inventoryDb)
    .WithReference(serviceBus)
    .WithReference(eventHubs)
    .WithReference(appInsights)
    .WithEnvironment("DatabaseProvider", "PostgreSQL")
    .WithHttpEndpoint(port: 5002)
    .WithHttpsEndpoint(port: 5012);

// Image Service (.NET 10)
var imageService = builder.AddProject<Projects.ImageService>("image-service")
    .WithReference(appInsights)
    .WithHttpEndpoint(port: 5003);

// Reporting Service (.NET 10)
var reportingService = builder.AddProject<Projects.ReportingService>("reporting-service")
    .WithReference(catalogDb)
    .WithReference(serviceBus)
    .WithReference(eventHubs)
    .WithReference(appInsights)
    .WithHttpEndpoint(port: 5004);

// ── BFF Layer ──

// Kotlin BFF (Ktor — Docker container)
var kotlinBff = builder.AddDockerfile("kotlin-bff", "../../templates/kotlin-bff-template")
    .WithReference(redis)
    .WithReference(appInsights)
    .WithEnvironment("CATALOG_SERVICE_URL", catalogService.GetEndpoint("http"))
    .WithEnvironment("INVENTORY_SERVICE_URL", inventoryService.GetEndpoint("http"))
    .WithHttpEndpoint(port: 8080, targetPort: 8080);

// React BFF (Next.js — via npm)
var reactBff = builder.AddNpmApp("react-bff", "../../templates/react-bff-template", "dev")
    .WithReference(redis)
    .WithEnvironment("CATALOG_SERVICE_URL", catalogService.GetEndpoint("http"))
    .WithEnvironment("INVENTORY_SERVICE_URL", inventoryService.GetEndpoint("http"))
    .WithHttpEndpoint(port: 3000, targetPort: 3000, env: "PORT");

// ── OPA Sidecar (Policy Engine) ──
var opa = builder.AddDockerfile("opa", "../../opa")
    .WithHttpEndpoint(port: 8181, targetPort: 8181);

// ── Build & Run ──
builder.Build().Run();

// ── Project Type Stubs (replace with actual project references) ──
// These are placeholders — update with real project paths
namespace Projects
{
    public class CatalogService { }
    public class InventoryService { }
    public class ImageService { }
    public class ReportingService { }
}
