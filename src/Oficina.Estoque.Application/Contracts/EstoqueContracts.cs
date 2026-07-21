namespace Oficina.Estoque.Application.Contracts;

public sealed record AjustarEstoqueRequest(int Quantidade);
public sealed record EstoqueResponse(IReadOnlyList<EstoquePecaResponse> Pecas, IReadOnlyList<EstoqueInsumoResponse> Insumos);
public sealed record EstoquePecaResponse(Guid PecaId, string? Descricao, int Quantidade);
public sealed record EstoqueInsumoResponse(Guid InsumoId, string? Descricao, int Quantidade);
