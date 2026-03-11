using eShopModernized.Data;
using eShopModernized.Models;
using eShopModernized.Repositories;
using eShopModernized.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

// ============================================================================
// Program.cs — Modernized entry point (replaces Global.asax.cs)
// 
// MIGRATION NOTES:
//   - Global.asax Application_Start()     → WebApplication.CreateBuilder()
//   - Autofac ContainerBuilder            → builder.Services (built-in DI)
//   - RouteConfig.RegisterRoutes()        → app.MapControllerRoute()
//   - BundleConfig.RegisterBundles()      → Static file middleware
//   - Web.config <connectionStrings>      → appsettings.json + User Secrets
//   - Web.config <appSettings>            → IOptions<CatalogSettings>
//   - log4net                             → Serilog structured logging
//   - ApplicationInsights.config          → OpenTelemetry configuration
// ============================================================================

var builder = WebApplication.CreateBuilder(args);

// ---- Logging (Replaces log4net + Application Insights) ----
// SECURITY FIX: Structured logging with PII scrubbing replaces
// log4net that was logging raw URLs and User-Agent strings
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"));

// ---- Configuration (Replaces Web.config + ConfigurationManager) ----
// SECURITY FIX: Connection strings no longer in source control.
// Uses User Secrets in development, environment variables in production.
builder.Services.Configure<CatalogSettings>(
    builder.Configuration.GetSection("CatalogSettings"));

// ---- Database Context (Replaces CatalogDBContext with EF6) ----
// SECURITY FIX: Uses parameterized queries only, no raw SQL.
// MIGRATION: EF6 DbContext → EF Core DbContext with HiLo sequences built-in.
// MIGRATION: Dual provider support — SQL Server or PostgreSQL via DatabaseProvider config.
var dbProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "SqlServer";
var catalogConnectionString = builder.Configuration.GetConnectionString("CatalogDb")!;

builder.Services.AddDbContext<CatalogDbContext>(options =>
{
    if (dbProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
    {
        options.UseNpgsql(catalogConnectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
        });
    }
    else
    {
        options.UseSqlServer(catalogConnectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });
    }
});

// ---- Dependency Injection (Replaces Autofac ApplicationModule) ----
// MIGRATION MAP:
//   Autofac InstancePerLifetimeScope → Scoped
//   Autofac SingleInstance            → Singleton
//   CatalogService                   → ICatalogRepository (repository pattern)
//   CatalogServiceSP                 → ELIMINATED (SPs migrated to EF Core)
//   CatalogItemHiLoGenerator         → ELIMINATED (EF Core UseHiLo built-in)
//   CatalogDBInitializer             → EF Core migrations + HasData seeding
builder.Services.AddScoped<ICatalogRepository, CatalogRepository>();
builder.Services.AddScoped<IInventoryService, InventoryService>();

// ---- MVC + API (Replaces App_Start/*.cs configuration) ----
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // SECURITY FIX: System.Text.Json has built-in MaxDepth (default: 64)
        // Replaces Newtonsoft.Json which had CVE-2024-21907 (DoS via deep nesting)
        options.JsonSerializerOptions.MaxDepth = 32;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// ---- Swagger / OpenAPI ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "eShop Catalog API", Version = "v1" });
});

// ---- Health Checks ----
var healthChecks = builder.Services.AddHealthChecks();
if (dbProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
{
    healthChecks.AddNpgSql(
        catalogConnectionString,
        name: "catalog-db",
        tags: new[] { "ready" });
}
else
{
    healthChecks.AddSqlServer(
        catalogConnectionString,
        name: "catalog-db",
        tags: new[] { "ready" });
}

// ---- CORS ----
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// ---- Middleware Pipeline (Replaces Application_BeginRequest) ----

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // SECURITY: HSTS for HTTPS enforcement
    app.UseHsts();
}

// SECURITY: Force HTTPS
app.UseHttpsRedirection();

// SECURITY: Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

app.UseStaticFiles();
app.UseRouting();
app.UseCors();
app.UseAuthorization();

// Serilog request logging (structured, no PII)
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        // NOTE: User-Agent intentionally NOT logged (PII under GDPR)
    };
});

// ---- Routes ----
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Catalog}/{action=Index}/{id?}");

app.MapHealthChecks("/health");
app.MapHealthChecks("/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// ---- Database Initialization ----
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Applying database migrations...");
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error applying database migrations. Ensuring database is created...");
        await context.Database.EnsureCreatedAsync();
    }
}

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
