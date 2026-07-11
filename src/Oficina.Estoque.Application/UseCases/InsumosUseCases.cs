using FluentValidation;
using Oficina.Estoque.Application.Abstractions;
using Oficina.Estoque.Application.Contracts;
using Oficina.Estoque.Application.Shared;
using Oficina.Estoque.Domain.CatalogoEstoque;

namespace Oficina.Estoque.Application.UseCases;

public sealed class InsumosUseCases(IEstoqueRepository repository, IValidator<CadastrarInsumoRequest> cadastrarValidator, IValidator<AtualizarInsumoRequest> atualizarValidator)
{
    public async Task<IReadOnlyList<InsumoResponse>> ListarAsync(CancellationToken ct)
    {
        var insumos = await repository.ListarInsumosAsync(ct);
        return insumos.Select(Map).ToList();
    }

    public async Task<InsumoResponse> ObterAsync(Guid id, CancellationToken ct)
    {
        var insumo = await repository.ObterInsumoAsync(id, ct)
            ?? throw EstoqueException.NotFound("Insumo nao encontrado.");

        return Map(insumo);
    }

    public async Task<Guid> CadastrarAsync(CadastrarInsumoRequest request, CancellationToken ct)
    {
        await cadastrarValidator.ValidateAndThrowAsync(request, ct);

        var insumo = new Insumo(request.PrecoUnitario, request.Descricao);
        repository.AdicionarInsumo(insumo);
        repository.AdicionarEstoqueInsumo(new EstoqueInsumo(insumo.Id, 0));
        await repository.SalvarAsync(ct);
        return insumo.Id;
    }

    public async Task AtualizarAsync(Guid id, AtualizarInsumoRequest request, CancellationToken ct)
    {
        await atualizarValidator.ValidateAndThrowAsync(request, ct);

        var insumo = await repository.ObterInsumoAsync(id, ct)
            ?? throw EstoqueException.NotFound("Insumo nao encontrado.");

        insumo.Atualizar(request.PrecoUnitario, request.Descricao);
        await repository.SalvarAsync(ct);
    }

    private static InsumoResponse Map(Insumo insumo) => new(insumo.Id, insumo.Descricao, insumo.PrecoUnitario);
}
