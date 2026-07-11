using Oficina.Estoque.Application.Abstractions;
using Oficina.Estoque.Application.Contracts;
using Oficina.Estoque.Domain.CatalogoEstoque;

namespace Oficina.Estoque.Application.UseCases;

public sealed class ConsultarMateriaisUseCase(IEstoqueRepository repository)
{
    public async Task<IReadOnlyList<MaterialInternalResponse>> ExecutarAsync(IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        var normalizados = ids.Where(x => x != Guid.Empty).Distinct().ToList();
        var responses = new List<MaterialInternalResponse>();
        foreach (var id in normalizados)
        {
            var peca = await repository.ObterPecaAsync(id, ct);
            if (peca is not null)
            {
                responses.Add(new MaterialInternalResponse(peca.Id, peca.Descricao, peca.PrecoUnitario, TipoMaterial.Peca));
                continue;
            }

            var insumo = await repository.ObterInsumoAsync(id, ct);
            if (insumo is not null)
                responses.Add(new MaterialInternalResponse(insumo.Id, insumo.Descricao, insumo.PrecoUnitario, TipoMaterial.Insumo));
        }

        return responses;
    }
}
