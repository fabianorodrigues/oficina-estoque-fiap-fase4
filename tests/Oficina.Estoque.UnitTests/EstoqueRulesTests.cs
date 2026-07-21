using FluentValidation;
using Oficina.Estoque.Application.Abstractions;
using Oficina.Estoque.Application.Contracts;
using Oficina.Estoque.Application.Shared;
using Oficina.Estoque.Application.UseCases;
using Oficina.Estoque.Application.Validators;
using Oficina.Estoque.Domain.CatalogoEstoque;
using Oficina.Estoque.Domain.Movimentacoes;
using Oficina.Estoque.Domain.Reservas;

namespace Oficina.Estoque.UnitTests;

public sealed class EstoqueRulesTests
{
    [Fact]
    public async Task Ajuste_positivo_incrementa_saldo_e_registra_movimentacao()
    {
        var repo = new FakeEstoqueRepository();
        var peca = repo.AddPecaComSaldo(10);
        var useCase = new EstoqueUseCases(repo, new AjustarEstoqueRequestValidator());

        await useCase.AjustarPecaAsync(peca.Id, new AjustarEstoqueRequest(5), CancellationToken.None);

        Assert.Equal(15, (await repo.ObterEstoquePecaAsync(peca.Id, CancellationToken.None))!.Quantidade);
        Assert.Contains(repo.Movimentacoes, x => x.Tipo == TipoMovimentacaoEstoque.Entrada && x.SaldoResultante == 15);
    }

    [Fact]
    public async Task Ajuste_negativo_decrementa_saldo()
    {
        var repo = new FakeEstoqueRepository();
        var peca = repo.AddPecaComSaldo(10);
        var useCase = new EstoqueUseCases(repo, new AjustarEstoqueRequestValidator());

        await useCase.AjustarPecaAsync(peca.Id, new AjustarEstoqueRequest(-3), CancellationToken.None);

        Assert.Equal(7, (await repo.ObterEstoquePecaAsync(peca.Id, CancellationToken.None))!.Quantidade);
        Assert.Contains(repo.Movimentacoes, x => x.Tipo == TipoMovimentacaoEstoque.Saida && x.Quantidade == 3);
    }

    [Fact]
    public async Task Ajuste_negativo_nao_permite_saldo_menor_que_zero()
    {
        var repo = new FakeEstoqueRepository();
        var peca = repo.AddPecaComSaldo(2);
        var useCase = new EstoqueUseCases(repo, new AjustarEstoqueRequestValidator());

        await Assert.ThrowsAsync<EstoqueException>(() =>
            useCase.AjustarPecaAsync(peca.Id, new AjustarEstoqueRequest(-3), CancellationToken.None));
        Assert.Equal(2, (await repo.ObterEstoquePecaAsync(peca.Id, CancellationToken.None))!.Quantidade);
    }

    [Fact]
    public async Task Material_inexistente_recusa_toda_reserva()
    {
        var repo = new FakeEstoqueRepository();
        var peca = repo.AddPecaComSaldo(10);
        var useCase = new ReservasUseCases(repo, new ReservarEstoqueRequestValidator());

        await Assert.ThrowsAsync<EstoqueException>(() => useCase.ReservarAsync(new ReservarEstoqueRequest(
            "os-1",
            [new(TipoMaterial.Peca, peca.Id, 3), new(TipoMaterial.Insumo, Guid.NewGuid(), 1)]), CancellationToken.None));

        Assert.Equal(10, (await repo.ObterEstoquePecaAsync(peca.Id, CancellationToken.None))!.Quantidade);
    }

    [Fact]
    public async Task Saldo_insuficiente_recusa_toda_reserva_sem_debito_parcial()
    {
        var repo = new FakeEstoqueRepository();
        var peca = repo.AddPecaComSaldo(10);
        var insumo = repo.AddInsumoComSaldo(1);
        var useCase = new ReservasUseCases(repo, new ReservarEstoqueRequestValidator());

        await Assert.ThrowsAsync<EstoqueException>(() => useCase.ReservarAsync(new ReservarEstoqueRequest(
            "os-2",
            [new(TipoMaterial.Peca, peca.Id, 3), new(TipoMaterial.Insumo, insumo.Id, 2)]), CancellationToken.None));

        Assert.Equal(10, (await repo.ObterEstoquePecaAsync(peca.Id, CancellationToken.None))!.Quantidade);
        Assert.Equal(1, (await repo.ObterEstoqueInsumoAsync(insumo.Id, CancellationToken.None))!.Quantidade);
    }

    [Fact]
    public async Task Reserva_duplicada_nao_baixa_novamente()
    {
        var repo = new FakeEstoqueRepository();
        var peca = repo.AddPecaComSaldo(10);
        var useCase = new ReservasUseCases(repo, new ReservarEstoqueRequestValidator());
        var request = new ReservarEstoqueRequest("os-3", [new(TipoMaterial.Peca, peca.Id, 4)]);

        var primeira = await useCase.ReservarAsync(request, CancellationToken.None);
        var segunda = await useCase.ReservarAsync(request, CancellationToken.None);

        Assert.False(primeira.Duplicada);
        Assert.True(segunda.Duplicada);
        Assert.Equal(6, (await repo.ObterEstoquePecaAsync(peca.Id, CancellationToken.None))!.Quantidade);
    }

    [Fact]
    public async Task Liberacao_restaura_saldo_uma_unica_vez()
    {
        var repo = new FakeEstoqueRepository();
        var peca = repo.AddPecaComSaldo(10);
        var useCase = new ReservasUseCases(repo, new ReservarEstoqueRequestValidator());
        var reserva = await useCase.ReservarAsync(new ReservarEstoqueRequest("os-4", [new(TipoMaterial.Peca, peca.Id, 4)]), CancellationToken.None);

        var primeira = await useCase.LiberarAsync(reserva.ReservaId, CancellationToken.None);
        var segunda = await useCase.LiberarAsync(reserva.ReservaId, CancellationToken.None);

        Assert.False(primeira.JaLiberada);
        Assert.True(segunda.JaLiberada);
        Assert.Equal(10, (await repo.ObterEstoquePecaAsync(peca.Id, CancellationToken.None))!.Quantidade);
        Assert.Single(repo.Movimentacoes, x => x.Tipo == TipoMovimentacaoEstoque.Liberacao);
    }

    [Fact]
    public async Task Disponibilidade_e_informativa_e_nao_reserva()
    {
        var repo = new FakeEstoqueRepository();
        var peca = repo.AddPecaComSaldo(5);
        var useCase = new DisponibilidadeEstoqueUseCase(repo, new DisponibilidadeEstoqueRequestValidator());

        var response = await useCase.ExecutarAsync(new DisponibilidadeEstoqueRequest([new(TipoMaterial.Peca, peca.Id, 2)]), CancellationToken.None);

        Assert.True(response.Informational);
        Assert.True(response.Items.Single().AvailableNow);
        Assert.Equal(5, (await repo.ObterEstoquePecaAsync(peca.Id, CancellationToken.None))!.Quantidade);
    }

    [Fact]
    public void Validators_rejeitam_ajuste_zero_e_cadastro_invalido()
    {
        Assert.False(new AjustarEstoqueRequestValidator().Validate(new AjustarEstoqueRequest(0)).IsValid);
        Assert.False(new CadastrarPecaRequestValidator().Validate(new CadastrarPecaRequest(-1, "")).IsValid);
    }

    [Fact]
    public async Task Cadastro_de_peca_cria_material_com_saldo_zero()
    {
        var repo = new FakeEstoqueRepository();
        var useCase = new PecasUseCases(repo, new CadastrarPecaRequestValidator(), new AtualizarPecaRequestValidator());

        var id = await useCase.CadastrarAsync(new CadastrarPecaRequest(12.5m, "Pastilha"), CancellationToken.None);

        var peca = await useCase.ObterAsync(id, CancellationToken.None);
        Assert.Equal("Pastilha", peca.Descricao);
        Assert.Equal(0, (await repo.ObterEstoquePecaAsync(id, CancellationToken.None))!.Quantidade);
    }

    [Fact]
    public async Task Cadastro_de_insumo_cria_material_com_saldo_zero()
    {
        var repo = new FakeEstoqueRepository();
        var useCase = new InsumosUseCases(repo, new CadastrarInsumoRequestValidator(), new AtualizarInsumoRequestValidator());

        var id = await useCase.CadastrarAsync(new CadastrarInsumoRequest(7m, "Graxa"), CancellationToken.None);

        var insumo = await useCase.ObterAsync(id, CancellationToken.None);
        Assert.Equal("Graxa", insumo.Descricao);
        Assert.Equal(0, (await repo.ObterEstoqueInsumoAsync(id, CancellationToken.None))!.Quantidade);
    }

    [Fact]
    public async Task Atualizacao_de_peca_e_insumo_preserva_contrato()
    {
        var repo = new FakeEstoqueRepository();
        var peca = repo.AddPecaComSaldo(1);
        var insumo = repo.AddInsumoComSaldo(1);

        await new PecasUseCases(repo, new CadastrarPecaRequestValidator(), new AtualizarPecaRequestValidator())
            .AtualizarAsync(peca.Id, new AtualizarPecaRequest(20m, "Filtro atualizado"), CancellationToken.None);
        await new InsumosUseCases(repo, new CadastrarInsumoRequestValidator(), new AtualizarInsumoRequestValidator())
            .AtualizarAsync(insumo.Id, new AtualizarInsumoRequest(3m, "Oleo atualizado"), CancellationToken.None);

        Assert.Equal("Filtro atualizado", (await repo.ObterPecaAsync(peca.Id, CancellationToken.None))!.Descricao);
        Assert.Equal("Oleo atualizado", (await repo.ObterInsumoAsync(insumo.Id, CancellationToken.None))!.Descricao);
    }

    [Fact]
    public async Task Consultas_de_estoque_retorna_descricoes()
    {
        var repo = new FakeEstoqueRepository();
        var peca = repo.AddPecaComSaldo(8);
        var insumo = repo.AddInsumoComSaldo(6);
        var useCase = new EstoqueUseCases(repo, new AjustarEstoqueRequestValidator());

        var estoque = await useCase.ListarAsync(CancellationToken.None);
        var estoquePeca = await useCase.ObterPecaAsync(peca.Id, CancellationToken.None);
        var estoqueInsumo = await useCase.ObterInsumoAsync(insumo.Id, CancellationToken.None);

        Assert.Single(estoque.Pecas);
        Assert.Single(estoque.Insumos);
        Assert.Equal("Filtro", estoquePeca.Descricao);
        Assert.Equal("Oleo", estoqueInsumo.Descricao);
    }

    [Fact]
    public async Task Consultas_inexistentes_retornam_erro_publico()
    {
        var repo = new FakeEstoqueRepository();

        await Assert.ThrowsAsync<EstoqueException>(() =>
            new PecasUseCases(repo, new CadastrarPecaRequestValidator(), new AtualizarPecaRequestValidator()).ObterAsync(Guid.NewGuid(), CancellationToken.None));
        await Assert.ThrowsAsync<EstoqueException>(() =>
            new InsumosUseCases(repo, new CadastrarInsumoRequestValidator(), new AtualizarInsumoRequestValidator()).ObterAsync(Guid.NewGuid(), CancellationToken.None));
        await Assert.ThrowsAsync<EstoqueException>(() =>
            new EstoqueUseCases(repo, new AjustarEstoqueRequestValidator()).ObterPecaAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public void Dominio_rejeita_valores_invalidos()
    {
        Assert.Throws<ArgumentException>(() => new Peca(1, ""));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Insumo(-1, "Insumo"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EstoquePeca(Guid.NewGuid(), -1));
        Assert.Throws<ArgumentException>(() => new ReservaEstoque("", [new ItemReservaEstoque(TipoMaterial.Peca, Guid.NewGuid(), 1)]));
        Assert.Throws<ArgumentException>(() => new MovimentacaoEstoque(TipoMaterial.Peca, Guid.NewGuid(), TipoMovimentacaoEstoque.Entrada, 1, 1, ""));
    }
}

internal sealed class FakeEstoqueRepository : IEstoqueRepository
{
    private readonly List<Peca> _pecas = [];
    private readonly List<Insumo> _insumos = [];
    private readonly List<EstoquePeca> _estoquePecas = [];
    private readonly List<EstoqueInsumo> _estoqueInsumos = [];
    private readonly List<ReservaEstoque> _reservas = [];

    public List<MovimentacaoEstoque> Movimentacoes { get; } = [];

    public Peca AddPecaComSaldo(int saldo)
    {
        var peca = new Peca(10, "Filtro");
        _pecas.Add(peca);
        _estoquePecas.Add(new EstoquePeca(peca.Id, saldo));
        return peca;
    }

    public Insumo AddInsumoComSaldo(int saldo)
    {
        var insumo = new Insumo(2, "Oleo");
        _insumos.Add(insumo);
        _estoqueInsumos.Add(new EstoqueInsumo(insumo.Id, saldo));
        return insumo;
    }

    public Task<IReadOnlyList<Peca>> ListarPecasAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Peca>>(_pecas);
    public Task<Peca?> ObterPecaAsync(Guid id, CancellationToken ct) => Task.FromResult(_pecas.FirstOrDefault(x => x.Id == id));
    public void AdicionarPeca(Peca peca) => _pecas.Add(peca);
    public Task<IReadOnlyList<Insumo>> ListarInsumosAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Insumo>>(_insumos);
    public Task<Insumo?> ObterInsumoAsync(Guid id, CancellationToken ct) => Task.FromResult(_insumos.FirstOrDefault(x => x.Id == id));
    public void AdicionarInsumo(Insumo insumo) => _insumos.Add(insumo);
    public Task<IReadOnlyList<EstoquePeca>> ListarEstoquePecasAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<EstoquePeca>>(_estoquePecas);
    public Task<IReadOnlyList<EstoqueInsumo>> ListarEstoqueInsumosAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<EstoqueInsumo>>(_estoqueInsumos);
    public Task<EstoquePeca?> ObterEstoquePecaAsync(Guid pecaId, CancellationToken ct) => Task.FromResult(_estoquePecas.FirstOrDefault(x => x.PecaId == pecaId));
    public Task<EstoqueInsumo?> ObterEstoqueInsumoAsync(Guid insumoId, CancellationToken ct) => Task.FromResult(_estoqueInsumos.FirstOrDefault(x => x.InsumoId == insumoId));
    public Task<EstoqueItem?> ObterEstoqueItemAsync(TipoMaterial tipoMaterial, Guid materialId, CancellationToken ct) => Task.FromResult(tipoMaterial == TipoMaterial.Peca ? _estoquePecas.FirstOrDefault(x => x.PecaId == materialId) : _estoqueInsumos.FirstOrDefault(x => x.InsumoId == materialId) as EstoqueItem);
    public void AdicionarEstoquePeca(EstoquePeca estoque) => _estoquePecas.Add(estoque);
    public void AdicionarEstoqueInsumo(EstoqueInsumo estoque) => _estoqueInsumos.Add(estoque);
    public Task<ReservaEstoque?> ObterReservaPorChaveAsync(string chaveOperacao, CancellationToken ct) => Task.FromResult(_reservas.FirstOrDefault(x => x.ChaveOperacao == chaveOperacao));
    public Task<ReservaEstoque?> ObterReservaAsync(Guid id, CancellationToken ct) => Task.FromResult(_reservas.FirstOrDefault(x => x.Id == id));
    public void AdicionarReserva(ReservaEstoque reserva) => _reservas.Add(reserva);
    public Task<IReadOnlyList<MovimentacaoEstoque>> ListarMovimentacoesAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<MovimentacaoEstoque>>(Movimentacoes);
    public void AdicionarMovimentacao(MovimentacaoEstoque movimentacao) => Movimentacoes.Add(movimentacao);
    public Task SalvarAsync(CancellationToken ct) => Task.CompletedTask;
}
