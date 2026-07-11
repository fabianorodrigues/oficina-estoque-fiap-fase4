using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Oficina.Estoque.Application.UseCases;

namespace Oficina.Estoque.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddEstoqueApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<PecasUseCases>();
        services.AddScoped<InsumosUseCases>();
        services.AddScoped<EstoqueUseCases>();
        services.AddScoped<DisponibilidadeEstoqueUseCase>();
        services.AddScoped<ReservasUseCases>();
        services.AddScoped<ConsultarMateriaisUseCase>();
        return services;
    }
}
