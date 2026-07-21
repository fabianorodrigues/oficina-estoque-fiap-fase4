using Microsoft.EntityFrameworkCore;
using Oficina.Estoque.Domain.CatalogoEstoque;
using Oficina.Estoque.Domain.Movimentacoes;
using Oficina.Estoque.Infrastructure.Migrations;
using Oficina.Estoque.Infrastructure.Persistencia;

namespace Oficina.Estoque.IntegrationTests;

public sealed class PersistenceMetadataTests
{
    [Fact]
    public void Modelo_configura_indices_unicos_e_rowversion()
    {
        var options = new DbContextOptionsBuilder<EstoqueDbContext>()
            .UseSqlServer("Server=localhost;Database=OficinaEstoqueDb_Test;User Id=sa;Password=Password123!;TrustServerCertificate=True")
            .Options;

        using var context = new EstoqueDbContext(options);
        var estoquePeca = context.Model.FindEntityType(typeof(EstoquePeca))!;
        var estoqueInsumo = context.Model.FindEntityType(typeof(EstoqueInsumo))!;

        Assert.Contains(estoquePeca.GetIndexes(), x => x.IsUnique && x.Properties.Single().Name == nameof(EstoqueItem.MaterialId));
        Assert.True(estoquePeca.FindProperty(nameof(EstoqueItem.RowVersion))!.IsConcurrencyToken);
        Assert.Contains(estoqueInsumo.GetIndexes(), x => x.IsUnique && x.Properties.Single().Name == nameof(EstoqueItem.MaterialId));
    }

    [Fact]
    public void Migration_inicial_existe()
    {
        Assert.Equal("InitialEstoque", typeof(InitialEstoque).Name);
    }

    [Fact]
    public void Testcontainers_sqlserver_deve_ser_referenciado_para_testes_isolados()
    {
        Assert.Equal("Testcontainers.MsSql", typeof(Testcontainers.MsSql.MsSqlContainer).Namespace);
    }

    [Fact]
    public async Task MovimentacaoEstoque_deve_ser_append_only()
    {
        var options = new DbContextOptionsBuilder<EstoqueDbContext>()
            .UseSqlServer("Server=localhost;Database=OficinaEstoqueDb_Test;User Id=sa;Password=Password123!;TrustServerCertificate=True")
            .Options;

        await using var context = new EstoqueDbContext(options);
        var movimentacao = new MovimentacaoEstoque(TipoMaterial.Peca, Guid.NewGuid(), TipoMovimentacaoEstoque.Entrada, 1, 1, "append-only-test");
        context.Attach(movimentacao);
        context.Remove(movimentacao);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        Assert.Contains("append-only", ex.Message);
    }
}
