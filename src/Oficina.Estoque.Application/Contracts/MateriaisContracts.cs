using Oficina.Estoque.Domain.CatalogoEstoque;

namespace Oficina.Estoque.Application.Contracts;

public sealed record ConsultarMateriaisRequest(IReadOnlyList<Guid> Ids);
public sealed record MaterialInternalResponse(Guid Id, string Descricao, decimal PrecoUnitario, TipoMaterial Tipo);
