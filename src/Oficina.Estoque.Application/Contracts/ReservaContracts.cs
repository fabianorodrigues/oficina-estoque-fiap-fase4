using Oficina.Estoque.Domain.CatalogoEstoque;

namespace Oficina.Estoque.Application.Contracts;

public sealed record ReservarEstoqueRequest(string ChaveOperacao, IReadOnlyList<ReservarEstoqueItemRequest> Itens);
public sealed record ReservarEstoqueItemRequest(TipoMaterial TipoMaterial, Guid MaterialId, int Quantidade);
public sealed record ReservarEstoqueResponse(Guid ReservaId, bool Duplicada);
public sealed record LiberarReservaEstoqueResponse(Guid ReservaId, bool JaLiberada);
