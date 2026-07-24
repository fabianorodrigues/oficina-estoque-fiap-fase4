using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Oficina.Estoque.Api.Middleware;
using Oficina.Estoque.Api.Observability;
using Oficina.Estoque.Api.Security;
using Oficina.Estoque.Application;
using Oficina.Estoque.Infrastructure;
using Oficina.Estoque.Infrastructure.Persistencia;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.JsonWriterOptions = new JsonWriterOptions { Indented = false };
});

builder.Services.AddControllers();
builder.Services.AddEstoqueApplication();
builder.Services.AddEstoqueInfrastructure(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Oficina Estoque API",
        Version = "v1"
    });
});

builder.Services.AddOficinaAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy(Policies.ClienteOnly, policy => policy.RequireRole(PerfisAcesso.Cliente));
    options.AddPolicy(Policies.FuncionarioOuAdmin, policy => policy.RequireRole(PerfisAcesso.Funcionario, PerfisAcesso.Admin));
    options.AddPolicy(Policies.AdminOnly, policy => policy.RequireRole(PerfisAcesso.Admin));
    options.AddPolicy(Policies.ClienteOuAdmin, policy => policy.RequireRole(PerfisAcesso.Cliente, PerfisAcesso.Admin));
});

builder.Services.AddOpenTelemetryFailOpen(
    builder.Configuration,
    builder.Logging,
    serviceName: "oficina-estoque");

var app = builder.Build();

await ApplyMigrationsIfEnabled(app);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "oficina-estoque" }))
    .AllowAnonymous();
app.MapGet("/ready", () => Results.Ok(new { status = "Ready", service = "oficina-estoque" }))
    .AllowAnonymous();

app.MapControllers();

app.Run();

static async Task ApplyMigrationsIfEnabled(WebApplication app)
{
    var enabled = app.Configuration.GetValue("Database:ApplyMigrations", false);
    if (!enabled)
        return;

    if (!app.Environment.IsDevelopment())
        throw new InvalidOperationException("Database__ApplyMigrations=true so pode ser usado em Development.");

    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<EstoqueDbContext>().Database.MigrateAsync();
}

public partial class Program;
