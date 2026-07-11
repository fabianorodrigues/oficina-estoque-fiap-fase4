using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oficina.Estoque.Application.Abstractions;
using Oficina.Estoque.Infrastructure.Messaging;
using Oficina.Estoque.Infrastructure.Persistencia;

namespace Oficina.Estoque.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddEstoqueInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("OficinaEstoqueDb")
            ?? "Server=(localdb)\\mssqllocaldb;Database=OficinaEstoqueDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        services.AddDbContext<EstoqueDbContext>(options => options
            .UseSqlServer(connectionString)
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning)));
        services.AddScoped<IEstoqueRepository, EstoqueRepository>();
        services.AddEstoqueMessaging(configuration);
        return services;
    }
}
