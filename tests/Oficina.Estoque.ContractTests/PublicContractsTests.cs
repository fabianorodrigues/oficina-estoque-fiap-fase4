using Microsoft.AspNetCore.Mvc;
using Oficina.Estoque.Api.Controllers;
using Oficina.Estoque.Application.Contracts;

namespace Oficina.Estoque.ContractTests;

public sealed class PublicContractsTests
{
    [Theory]
    [InlineData(typeof(PecasController), "api/pecas")]
    [InlineData(typeof(InsumosController), "api/insumos")]
    [InlineData(typeof(EstoqueController), "api/estoque")]
    [InlineData(typeof(InternalEstoqueController), "api/internal/estoque")]
    public void Controllers_preservam_rotas(Type controllerType, string route)
    {
        var attribute = controllerType.GetCustomAttributes(typeof(RouteAttribute), inherit: false).Cast<RouteAttribute>().Single();
        Assert.Equal(route, attribute.Template);
    }

    [Fact]
    public void Contrato_disponibilidade_e_explicitamente_informativo()
    {
        var response = new DisponibilidadeEstoqueResponse(true, [new(Guid.NewGuid(), true, true, 10, 2)]);

        Assert.True(response.Informational);
        Assert.Equal(10, response.Items.Single().AvailableQuantity);
        Assert.Equal(2, response.Items.Single().RequestedQuantity);
    }
}
