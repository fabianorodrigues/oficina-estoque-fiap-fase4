using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Estoque.Api.Security;
using Oficina.Estoque.Application.Contracts;
using Oficina.Estoque.Application.UseCases;

namespace Oficina.Estoque.Api.Controllers;

[ApiController]
[Route("api/internal/estoque")]
[Authorize(Policy = Policies.FuncionarioOuAdmin)]
public sealed class InternalEstoqueController(
    DisponibilidadeEstoqueUseCase useCase,
    ConsultarMateriaisUseCase consultarMateriais) : ControllerBase
{
    [HttpPost("disponibilidade")]
    public async Task<ActionResult<DisponibilidadeEstoqueResponse>> ConsultarDisponibilidade(
        [FromBody] DisponibilidadeEstoqueRequest request,
        CancellationToken ct)
    {
        var response = await useCase.ExecutarAsync(request, ct);
        return Ok(response);
    }

    [HttpPost("/api/internal/materiais/consulta")]
    public async Task<ActionResult<IReadOnlyList<MaterialInternalResponse>>> ConsultarMateriais(
        [FromBody] ConsultarMateriaisRequest request,
        CancellationToken ct)
        => Ok(await consultarMateriais.ExecutarAsync(request.Ids, ct));
}
