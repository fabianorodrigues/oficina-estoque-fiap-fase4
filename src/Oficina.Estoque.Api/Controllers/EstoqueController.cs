using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Estoque.Api.Security;
using Oficina.Estoque.Application.Contracts;
using Oficina.Estoque.Application.UseCases;

namespace Oficina.Estoque.Api.Controllers;

[ApiController]
[Route("api/estoque")]
[Authorize(Policy = Policies.FuncionarioOuAdmin)]
public class EstoqueController(EstoqueUseCases useCases) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var estoque = await useCases.ListarAsync(ct);
        return Ok(estoque);
    }

    [HttpGet("pecas/{pecaId:guid}")]
    public async Task<IActionResult> ObterPeca(Guid pecaId, CancellationToken ct)
    {
        var estoque = await useCases.ObterPecaAsync(pecaId, ct);
        return Ok(estoque);
    }

    [HttpPost("pecas/{pecaId:guid}/ajustar")]
    public async Task<IActionResult> AjustarPeca(Guid pecaId, [FromBody] AjustarEstoqueRequest req, CancellationToken ct)
    {
        await useCases.AjustarPecaAsync(pecaId, req, ct);
        return NoContent();
    }

    [HttpGet("insumos/{insumoId:guid}")]
    public async Task<IActionResult> ObterInsumo(Guid insumoId, CancellationToken ct)
    {
        var estoque = await useCases.ObterInsumoAsync(insumoId, ct);
        return Ok(estoque);
    }

    [HttpPost("insumos/{insumoId:guid}/ajustar")]
    public async Task<IActionResult> AjustarInsumo(Guid insumoId, [FromBody] AjustarEstoqueRequest req, CancellationToken ct)
    {
        await useCases.AjustarInsumoAsync(insumoId, req, ct);
        return NoContent();
    }
}
