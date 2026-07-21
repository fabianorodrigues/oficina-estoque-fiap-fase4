namespace Oficina.Estoque.Application.Contracts;

public sealed record CadastrarInsumoRequest(decimal PrecoUnitario, string Descricao);
public sealed record AtualizarInsumoRequest(decimal PrecoUnitario, string Descricao);
public sealed record InsumoResponse(Guid Id, string Descricao, decimal PrecoUnitario);
