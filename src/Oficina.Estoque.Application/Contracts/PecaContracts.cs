namespace Oficina.Estoque.Application.Contracts;

public sealed record CadastrarPecaRequest(decimal PrecoUnitario, string Descricao);
public sealed record AtualizarPecaRequest(decimal PrecoUnitario, string Descricao);
public sealed record PecaResponse(Guid Id, string Descricao, decimal PrecoUnitario);
