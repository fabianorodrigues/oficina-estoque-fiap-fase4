using Oficina.Estoque.Domain.SharedKernel;

namespace Oficina.Estoque.Domain.CatalogoEstoque;

public abstract class EstoqueItem : AgregadoRaiz
{
    protected EstoqueItem() { }

    protected EstoqueItem(Guid materialId, int quantidadeInicial)
    {
        if (materialId == Guid.Empty)
            throw new ArgumentException("Material invalido.", nameof(materialId));
        if (quantidadeInicial < 0)
            throw new ArgumentOutOfRangeException(nameof(quantidadeInicial));

        MaterialId = materialId;
        Quantidade = quantidadeInicial;
    }

    public Guid MaterialId { get; protected set; }
    public int Quantidade { get; protected set; }
    public byte[] RowVersion { get; private set; } = [];

    public void Ajustar(int quantidade)
    {
        var novoSaldo = Quantidade + quantidade;
        if (novoSaldo < 0)
            throw new InvalidOperationException("Saldo nao pode ficar negativo.");

        Quantidade = novoSaldo;
    }

    public void Reservar(int quantidade)
    {
        if (quantidade <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantidade));
        if (Quantidade < quantidade)
            throw new InvalidOperationException("Saldo insuficiente.");

        Quantidade -= quantidade;
    }

    public void Liberar(int quantidade)
    {
        if (quantidade <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantidade));

        Quantidade += quantidade;
    }
}

public class EstoquePeca : EstoqueItem
{
    private EstoquePeca() { }
    public EstoquePeca(Guid pecaId, int quantidadeInicial) : base(pecaId, quantidadeInicial) { }
    public Guid PecaId => MaterialId;
}

public class EstoqueInsumo : EstoqueItem
{
    private EstoqueInsumo() { }
    public EstoqueInsumo(Guid insumoId, int quantidadeInicial) : base(insumoId, quantidadeInicial) { }
    public Guid InsumoId => MaterialId;
}
