using Oficina.Estoque.Domain.CatalogoEstoque;

namespace Oficina.Estoque.Application.Contracts;

public sealed record DisponibilidadeEstoqueRequest(IReadOnlyList<DisponibilidadeEstoqueItemRequest> Items);
public sealed record DisponibilidadeEstoqueItemRequest(TipoMaterial TipoMaterial, Guid MaterialId, int RequestedQuantity);
public sealed record DisponibilidadeEstoqueResponse(bool Informational, IReadOnlyList<DisponibilidadeEstoqueItemResponse> Items);
public sealed record DisponibilidadeEstoqueItemResponse(
    Guid MaterialId,
    bool Exists,
    bool AvailableNow,
    int AvailableQuantity,
    int RequestedQuantity);
