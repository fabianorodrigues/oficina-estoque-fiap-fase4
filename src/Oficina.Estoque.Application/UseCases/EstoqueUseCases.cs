using FluentValidation;
using Oficina.Estoque.Application.Abstractions;
using Oficina.Estoque.Application.Contracts;
using Oficina.Estoque.Application.Shared;
using Oficina.Estoque.Domain.CatalogoEstoque;
using Oficina.Estoque.Domain.Movimentacoes;

namespace Oficina.Estoque.Application.UseCases;

public sealed class EstoqueUseCases(IEstoqueRepository repository, IValidator<AjustarEstoqueRequest> ajusteValidator)
{
    public async Task<EstoqueResponse> ListarAsync(CancellationToken ct)
    {
        var pecas = await repository.ListarEstoquePecasAsync(ct);
        var insumos = await repository.ListarEstoqueInsumosAsync(ct);

        var pecaResponses = new List<EstoquePecaResponse>();
        foreach (var estoque in pecas)
        {
            var peca = await repository.ObterPecaAsync(estoque.PecaId, ct);
            pecaResponses.Add(new EstoquePecaResponse(estoque.PecaId, peca?.Descricao, estoque.Quantidade));
        }

        var insumoResponses = new List<EstoqueInsumoResponse>();
        foreach (var estoque in insumos)
        {
            var insumo = await repository.ObterInsumoAsync(estoque.InsumoId, ct);
            insumoResponses.Add(new EstoqueInsumoResponse(estoque.InsumoId, insumo?.Descricao, estoque.Quantidade));
        }

        return new EstoqueResponse(pecaResponses, insumoResponses);
    }

    public async Task<EstoquePecaResponse> ObterPecaAsync(Guid pecaId, CancellationToken ct)
    {
        var estoque = await repository.ObterEstoquePecaAsync(pecaId, ct)
            ?? throw EstoqueException.NotFound("Estoque da peca nao encontrado.");
        var peca = await repository.ObterPecaAsync(pecaId, ct);
        return new EstoquePecaResponse(pecaId, peca?.Descricao, estoque.Quantidade);
    }

    public async Task<EstoqueInsumoResponse> ObterInsumoAsync(Guid insumoId, CancellationToken ct)
    {
        var estoque = await repository.ObterEstoqueInsumoAsync(insumoId, ct)
            ?? throw EstoqueException.NotFound("Estoque do insumo nao encontrado.");
        var insumo = await repository.ObterInsumoAsync(insumoId, ct);
        return new EstoqueInsumoResponse(insumoId, insumo?.Descricao, estoque.Quantidade);
    }

    public Task AjustarPecaAsync(Guid pecaId, AjustarEstoqueRequest request, CancellationToken ct)
        => AjustarAsync(TipoMaterial.Peca, pecaId, request, ct);

    public Task AjustarInsumoAsync(Guid insumoId, AjustarEstoqueRequest request, CancellationToken ct)
        => AjustarAsync(TipoMaterial.Insumo, insumoId, request, ct);

    private async Task AjustarAsync(TipoMaterial tipoMaterial, Guid materialId, AjustarEstoqueRequest request, CancellationToken ct)
    {
        await ajusteValidator.ValidateAndThrowAsync(request, ct);

        var estoque = await repository.ObterEstoqueItemAsync(tipoMaterial, materialId, ct)
            ?? throw EstoqueException.NotFound("Material nao encontrado no estoque.");

        try
        {
            estoque.Ajustar(request.Quantidade);
        }
        catch (InvalidOperationException ex)
        {
            throw EstoqueException.Conflict(ex.Message);
        }

        repository.AdicionarMovimentacao(new MovimentacaoEstoque(
            tipoMaterial,
            materialId,
            request.Quantidade > 0 ? TipoMovimentacaoEstoque.Entrada : TipoMovimentacaoEstoque.Saida,
            Math.Abs(request.Quantidade),
            estoque.Quantidade,
            $"ajuste:{Guid.NewGuid()}"));

        await repository.SalvarAsync(ct);
    }
}
