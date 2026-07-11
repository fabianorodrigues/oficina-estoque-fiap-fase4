using Oficina.Estoque.Domain.CatalogoEstoque;
using Oficina.Estoque.Domain.Movimentacoes;
using Oficina.Estoque.Domain.Reservas;

namespace Oficina.Estoque.Application.Abstractions;

public interface IEstoqueRepository
{
    Task<IReadOnlyList<Peca>> ListarPecasAsync(CancellationToken ct);
    Task<Peca?> ObterPecaAsync(Guid id, CancellationToken ct);
    void AdicionarPeca(Peca peca);

    Task<IReadOnlyList<Insumo>> ListarInsumosAsync(CancellationToken ct);
    Task<Insumo?> ObterInsumoAsync(Guid id, CancellationToken ct);
    void AdicionarInsumo(Insumo insumo);

    Task<IReadOnlyList<EstoquePeca>> ListarEstoquePecasAsync(CancellationToken ct);
    Task<IReadOnlyList<EstoqueInsumo>> ListarEstoqueInsumosAsync(CancellationToken ct);
    Task<EstoquePeca?> ObterEstoquePecaAsync(Guid pecaId, CancellationToken ct);
    Task<EstoqueInsumo?> ObterEstoqueInsumoAsync(Guid insumoId, CancellationToken ct);
    Task<EstoqueItem?> ObterEstoqueItemAsync(TipoMaterial tipoMaterial, Guid materialId, CancellationToken ct);
    void AdicionarEstoquePeca(EstoquePeca estoque);
    void AdicionarEstoqueInsumo(EstoqueInsumo estoque);

    Task<ReservaEstoque?> ObterReservaPorChaveAsync(string chaveOperacao, CancellationToken ct);
    Task<ReservaEstoque?> ObterReservaAsync(Guid id, CancellationToken ct);
    void AdicionarReserva(ReservaEstoque reserva);

    Task<IReadOnlyList<MovimentacaoEstoque>> ListarMovimentacoesAsync(CancellationToken ct);
    void AdicionarMovimentacao(MovimentacaoEstoque movimentacao);

    Task SalvarAsync(CancellationToken ct);
}
