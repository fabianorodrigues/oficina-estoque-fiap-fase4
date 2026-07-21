using Microsoft.EntityFrameworkCore;
using Oficina.Estoque.Domain.CatalogoEstoque;
using Oficina.Estoque.Domain.Movimentacoes;
using Oficina.Estoque.Domain.Reservas;
using Oficina.Estoque.Infrastructure.Messaging;

namespace Oficina.Estoque.Infrastructure.Persistencia;

public sealed class EstoqueDbContext(DbContextOptions<EstoqueDbContext> options) : DbContext(options)
{
    public DbSet<Peca> Pecas => Set<Peca>();
    public DbSet<Insumo> Insumos => Set<Insumo>();
    public DbSet<EstoquePeca> EstoquePecas => Set<EstoquePeca>();
    public DbSet<EstoqueInsumo> EstoqueInsumos => Set<EstoqueInsumo>();
    public DbSet<ReservaEstoque> ReservasEstoque => Set<ReservaEstoque>();
    public DbSet<ItemReservaEstoque> ItensReservaEstoque => Set<ItemReservaEstoque>();
    public DbSet<MovimentacaoEstoque> MovimentacoesEstoque => Set<MovimentacaoEstoque>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        RejeitarAlteracaoDeMovimentacoes();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        RejeitarAlteracaoDeMovimentacoes();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void RejeitarAlteracaoDeMovimentacoes()
    {
        var temAlteracao = ChangeTracker.Entries<MovimentacaoEstoque>()
            .Any(e => e.State is EntityState.Modified or EntityState.Deleted);
        if (temAlteracao)
            throw new InvalidOperationException("MovimentacaoEstoque e append-only e nao pode ser alterada ou removida.");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Peca>(builder =>
        {
            builder.ToTable("Pecas");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Descricao).HasMaxLength(200).IsRequired();
            builder.Property(x => x.PrecoUnitario).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Insumo>(builder =>
        {
            builder.ToTable("Insumos");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Descricao).HasMaxLength(200).IsRequired();
            builder.Property(x => x.PrecoUnitario).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<EstoquePeca>(builder =>
        {
            builder.ToTable("EstoquePecas");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.MaterialId).HasColumnName("PecaId").IsRequired();
            builder.Property(x => x.Quantidade).IsRequired();
            builder.Property(x => x.RowVersion).IsRowVersion();
            builder.HasIndex(x => x.MaterialId).IsUnique();
        });

        modelBuilder.Entity<EstoqueInsumo>(builder =>
        {
            builder.ToTable("EstoqueInsumos");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.MaterialId).HasColumnName("InsumoId").IsRequired();
            builder.Property(x => x.Quantidade).IsRequired();
            builder.Property(x => x.RowVersion).IsRowVersion();
            builder.HasIndex(x => x.MaterialId).IsUnique();
        });

        modelBuilder.Entity<ReservaEstoque>(builder =>
        {
            builder.ToTable("ReservasEstoque");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ChaveOperacao).HasMaxLength(120).IsRequired();
            builder.Property(x => x.Status).HasConversion<int>().IsRequired();
            builder.Property(x => x.DataCriacao).IsRequired();
            builder.HasIndex(x => x.ChaveOperacao).IsUnique();
            builder.HasMany(x => x.Itens)
                .WithOne()
                .HasForeignKey(x => x.ReservaEstoqueId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(x => x.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ItemReservaEstoque>(builder =>
        {
            builder.ToTable("ItensReservaEstoque");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.TipoMaterial).HasConversion<int>().IsRequired();
            builder.Property(x => x.MaterialId).IsRequired();
            builder.Property(x => x.Quantidade).IsRequired();
            builder.HasIndex(x => new { x.ReservaEstoqueId, x.TipoMaterial, x.MaterialId }).IsUnique();
        });

        modelBuilder.Entity<MovimentacaoEstoque>(builder =>
        {
            builder.ToTable("MovimentacoesEstoque");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.TipoMaterial).HasConversion<int>().IsRequired();
            builder.Property(x => x.MaterialId).IsRequired();
            builder.Property(x => x.Tipo).HasConversion<int>().IsRequired();
            builder.Property(x => x.Quantidade).IsRequired();
            builder.Property(x => x.SaldoResultante).IsRequired();
            builder.Property(x => x.Data).IsRequired();
            builder.Property(x => x.ReferenciaOperacao).HasMaxLength(120).IsRequired();
            builder.HasIndex(x => new { x.TipoMaterial, x.MaterialId, x.Data });
            builder.HasIndex(x => x.ReferenciaOperacao);
        });

        modelBuilder.Entity<InboxMessage>(builder =>
        {
            builder.ToTable("InboxMessages");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.MessageType).HasMaxLength(120).IsRequired();
            builder.Property(x => x.CorrelationId).HasMaxLength(120).IsRequired();
            builder.Property(x => x.Body).HasColumnType("nvarchar(max)").IsRequired();
            builder.Property(x => x.Status).HasConversion<int>().IsRequired();
            builder.Property(x => x.Error).HasMaxLength(500);
            builder.HasIndex(x => x.MessageId).IsUnique();
            builder.HasIndex(x => new { x.Status, x.LockedUntilUtc, x.ReceivedAtUtc });
        });

        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.ToTable("OutboxMessages");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.MessageType).HasMaxLength(120).IsRequired();
            builder.Property(x => x.CorrelationId).HasMaxLength(120).IsRequired();
            builder.Property(x => x.CausationId).HasMaxLength(120);
            builder.Property(x => x.Body).HasColumnType("nvarchar(max)").IsRequired();
            builder.Property(x => x.Error).HasMaxLength(500);
            builder.HasIndex(x => x.MessageId).IsUnique();
            builder.HasIndex(x => new { x.PublishedAtUtc, x.LockedUntilUtc, x.CreatedAtUtc });
        });
    }
}
