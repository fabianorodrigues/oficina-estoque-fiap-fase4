using Microsoft.EntityFrameworkCore;
using Oficina.Estoque.Application.Abstractions;
using Oficina.Estoque.Domain.CatalogoEstoque;
using Oficina.Estoque.Domain.Movimentacoes;
using Oficina.Estoque.Domain.Reservas;

namespace Oficina.Estoque.Infrastructure.Persistencia;

public sealed class EstoqueRepository(EstoqueDbContext dbContext) : IEstoqueRepository
{
    public async Task<IReadOnlyList<Peca>> ListarPecasAsync(CancellationToken ct) =>
        await dbContext.Pecas.OrderBy(x => x.Descricao).ToListAsync(ct);

    public Task<Peca?> ObterPecaAsync(Guid id, CancellationToken ct) =>
        dbContext.Pecas.FirstOrDefaultAsync(x => x.Id == id, ct);

    public void AdicionarPeca(Peca peca) => dbContext.Pecas.Add(peca);

    public async Task<IReadOnlyList<Insumo>> ListarInsumosAsync(CancellationToken ct) =>
        await dbContext.Insumos.OrderBy(x => x.Descricao).ToListAsync(ct);

    public Task<Insumo?> ObterInsumoAsync(Guid id, CancellationToken ct) =>
        dbContext.Insumos.FirstOrDefaultAsync(x => x.Id == id, ct);

    public void AdicionarInsumo(Insumo insumo) => dbContext.Insumos.Add(insumo);

    public async Task<IReadOnlyList<EstoquePeca>> ListarEstoquePecasAsync(CancellationToken ct) =>
        await dbContext.EstoquePecas.OrderBy(x => x.MaterialId).ToListAsync(ct);

    public async Task<IReadOnlyList<EstoqueInsumo>> ListarEstoqueInsumosAsync(CancellationToken ct) =>
        await dbContext.EstoqueInsumos.OrderBy(x => x.MaterialId).ToListAsync(ct);

    public Task<EstoquePeca?> ObterEstoquePecaAsync(Guid pecaId, CancellationToken ct) =>
        dbContext.EstoquePecas.FirstOrDefaultAsync(x => x.MaterialId == pecaId, ct);

    public Task<EstoqueInsumo?> ObterEstoqueInsumoAsync(Guid insumoId, CancellationToken ct) =>
        dbContext.EstoqueInsumos.FirstOrDefaultAsync(x => x.MaterialId == insumoId, ct);

    public async Task<EstoqueItem?> ObterEstoqueItemAsync(TipoMaterial tipoMaterial, Guid materialId, CancellationToken ct) =>
        tipoMaterial switch
        {
            TipoMaterial.Peca => await ObterEstoquePecaAsync(materialId, ct),
            TipoMaterial.Insumo => await ObterEstoqueInsumoAsync(materialId, ct),
            _ => null
        };

    public void AdicionarEstoquePeca(EstoquePeca estoque) => dbContext.EstoquePecas.Add(estoque);

    public void AdicionarEstoqueInsumo(EstoqueInsumo estoque) => dbContext.EstoqueInsumos.Add(estoque);

    public Task<ReservaEstoque?> ObterReservaPorChaveAsync(string chaveOperacao, CancellationToken ct) =>
        dbContext.ReservasEstoque
            .Include(x => x.Itens)
            .FirstOrDefaultAsync(x => x.ChaveOperacao == chaveOperacao, ct);

    public Task<ReservaEstoque?> ObterReservaAsync(Guid id, CancellationToken ct) =>
        dbContext.ReservasEstoque
            .Include(x => x.Itens)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public void AdicionarReserva(ReservaEstoque reserva) => dbContext.ReservasEstoque.Add(reserva);

    public async Task<IReadOnlyList<MovimentacaoEstoque>> ListarMovimentacoesAsync(CancellationToken ct) =>
        await dbContext.MovimentacoesEstoque.OrderBy(x => x.Data).ToListAsync(ct);

    public void AdicionarMovimentacao(MovimentacaoEstoque movimentacao) =>
        dbContext.MovimentacoesEstoque.Add(movimentacao);

    public Task SalvarAsync(CancellationToken ct) => dbContext.SaveChangesAsync(ct);
}
