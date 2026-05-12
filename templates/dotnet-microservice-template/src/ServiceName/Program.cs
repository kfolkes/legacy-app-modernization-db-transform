// =============================================================================
// .NET 10 Microservice Template — Program.cs
// Follows: microservice-rules.md, opa-rbac-rules.md
// =============================================================================

using Microsoft.Identity.Web;
using Serilog;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// ── Logging (Serilog) ──
builder.Host.UseSerilog((context, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ServiceName", "ServiceName")
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj} {Properties:j}{NewLine}{Exception}"));

// ── Authentication (Entra ID) ──
builder.Services.AddAuthentication()
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

// ── Authorization (RBAC + OPA) ──
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Admin", policy => policy.RequireRole("Admin"))
    .AddPolicy("Reader", policy => policy.RequireAuthenticatedUser())
    .AddPolicy("OpaAuthorized", policy => policy.AddRequirements(new OpaAuthorizationRequirement()));

builder.Services.AddHttpClient("OPA", c =>
    c.BaseAddress = new Uri(builder.Configuration["Opa:Endpoint"] ?? "http://localhost:8181"));
builder.Services.AddSingleton<IAuthorizationHandler, OpaAuthorizationHandler>();

// ── Database (Dual Provider: SQL Server + PostgreSQL) ──
var dbProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "PostgreSQL";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ServiceDbContext>(options =>
{
    if (dbProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
        options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure());
    else
        options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
});

// ── Messaging (MassTransit + Azure Service Bus) ──
builder.Services.AddMassTransit(x =>
{
    x.AddConsumers(typeof(Program).Assembly);
    x.UsingAzureServiceBus((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("ServiceBus"));
        cfg.ConfigureEndpoints(context);
    });
});

// ── Observability (OpenTelemetry → Azure Monitor) ──
builder.Services.AddOpenTelemetry()
    .UseAzureMonitor(options =>
        options.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"])
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddGrpcClientInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("MassTransit"))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation());

// ── Health Checks ──
var healthBuilder = builder.Services.AddHealthChecks();
if (dbProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
    healthBuilder.AddNpgSql(connectionString!, name: "postgresql");
else
    healthBuilder.AddSqlServer(connectionString!, name: "sqlserver");

// ── API ──
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── gRPC ──
builder.Services.AddGrpc();

var app = builder.Build();

// ── Middleware Pipeline ──
app.UseHttpsRedirection();
app.UseHsts();

// SECURITY FIX: Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.UseSerilogRequestLogging();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/ready");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();

// ── OPA Authorization Support Types ──
public class OpaAuthorizationRequirement : IAuthorizationRequirement { }

public class OpaAuthorizationHandler : AuthorizationHandler<OpaAuthorizationRequirement>
{
    private readonly IHttpClientFactory _httpClientFactory;

    public OpaAuthorizationHandler(IHttpClientFactory httpClientFactory)
        => _httpClientFactory = httpClientFactory;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, OpaAuthorizationRequirement requirement)
    {
        if (context.Resource is not HttpContext httpContext) return;

        var opaInput = new
        {
            method = httpContext.Request.Method,
            path = httpContext.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries),
            roles = context.User.Claims
                .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value),
            user = new { sub = context.User.FindFirst("sub")?.Value }
        };

        var client = _httpClientFactory.CreateClient("OPA");
        var response = await client.PostAsJsonAsync("/v1/data/authz/allow", new { input = opaInput });

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<OpaResponse>();
            if (result?.Result == true)
                context.Succeed(requirement);
        }
    }
}

public record OpaResponse(bool Result);
