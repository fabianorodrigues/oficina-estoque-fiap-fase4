using FluentValidation;
using Oficina.Estoque.Application.Abstractions;
using Oficina.Estoque.Application.Contracts;
using Oficina.Estoque.Application.Shared;
using Oficina.Estoque.Domain.CatalogoEstoque;

namespace Oficina.Estoque.Application.UseCases;

public sealed class PecasUseCases(IEstoqueRepository repository, IValidator<CadastrarPecaRequest> cadastrarValidator, IValidator<AtualizarPecaRequest> atualizarValidator)
{
    public async Task<IReadOnlyList<PecaResponse>> ListarAsync(CancellationToken ct)
    {
        var pecas = await repository.ListarPecasAsync(ct);
        return pecas.Select(Map).ToList();
    }

    public async Task<PecaResponse> ObterAsync(Guid id, CancellationToken ct)
    {
        var peca = await repository.ObterPecaAsync(id, ct)
            ?? throw EstoqueException.NotFound("Peca nao encontrada.");

        return Map(peca);
    }

    public async Task<Guid> CadastrarAsync(CadastrarPecaRequest request, CancellationToken ct)
    {
        await cadastrarValidator.ValidateAndThrowAsync(request, ct);

        var peca = new Peca(request.PrecoUnitario, request.Descricao);
        repository.AdicionarPeca(peca);
        repository.AdicionarEstoquePeca(new EstoquePeca(peca.Id, 0));
        await repository.SalvarAsync(ct);
        return peca.Id;
    }

    public async Task AtualizarAsync(Guid id, AtualizarPecaRequest request, CancellationToken ct)
    {
        await atualizarValidator.ValidateAndThrowAsync(request, ct);

        var peca = await repository.ObterPecaAsync(id, ct)
            ?? throw EstoqueException.NotFound("Peca nao encontrada.");

        peca.Atualizar(request.PrecoUnitario, request.Descricao);
        await repository.SalvarAsync(ct);
    }

    private static PecaResponse Map(Peca peca) => new(peca.Id, peca.Descricao, peca.PrecoUnitario);
}
