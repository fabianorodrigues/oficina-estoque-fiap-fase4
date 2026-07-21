using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Estoque.Api.Security;
using Oficina.Estoque.Application.Contracts;
using Oficina.Estoque.Application.UseCases;

namespace Oficina.Estoque.Api.Controllers;

[ApiController]
[Route("api/insumos")]
[Authorize(Policy = Policies.FuncionarioOuAdmin)]
public class InsumosController(InsumosUseCases useCases) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var insumos = await useCases.ListarAsync(ct);
        return Ok(insumos.Select(v => new { v.Id, v.Descricao, v.PrecoUnitario }));
    }

    [HttpPost]
    public async Task<IActionResult> Cadastrar([FromBody] CadastrarInsumoRequest req, CancellationToken ct)
    {
        var id = await useCases.CadastrarAsync(req, ct);
        return CreatedAtAction(nameof(ObterPorId), new { id }, new { id });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken ct)
    {
        var insumo = await useCases.ObterAsync(id, ct);
        return Ok(new { insumo.Id, insumo.Descricao, insumo.PrecoUnitario });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarInsumoRequest req, CancellationToken ct)
    {
        await useCases.AtualizarAsync(id, req, ct);
        return NoContent();
    }
}
