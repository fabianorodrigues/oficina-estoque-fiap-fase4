using FluentValidation;
using Oficina.Estoque.Application.Abstractions;
using Oficina.Estoque.Application.Contracts;

namespace Oficina.Estoque.Application.UseCases;

public sealed class DisponibilidadeEstoqueUseCase(IEstoqueRepository repository, IValidator<DisponibilidadeEstoqueRequest> validator)
{
    public async Task<DisponibilidadeEstoqueResponse> ExecutarAsync(DisponibilidadeEstoqueRequest request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);

        var responses = new List<DisponibilidadeEstoqueItemResponse>();
        foreach (var item in request.Items)
        {
            var estoque = await repository.ObterEstoqueItemAsync(item.TipoMaterial, item.MaterialId, ct);
            responses.Add(new DisponibilidadeEstoqueItemResponse(
                item.MaterialId,
                estoque is not null,
                estoque is not null && estoque.Quantidade >= item.RequestedQuantity,
                estoque?.Quantidade ?? 0,
                item.RequestedQuantity));
        }

        return new DisponibilidadeEstoqueResponse(true, responses);
    }
}
