using Oficina.Estoque.Domain.CatalogoEstoque;
using Oficina.Estoque.Domain.SharedKernel;

namespace Oficina.Estoque.Domain.Reservas;

public enum StatusReservaEstoque
{
    Reservada = 1,
    Liberada = 2
}

public class ReservaEstoque : AgregadoRaiz
{
    private readonly List<ItemReservaEstoque> _itens = [];

    private ReservaEstoque() { }

    public ReservaEstoque(string chaveOperacao, IEnumerable<ItemReservaEstoque> itens)
    {
        if (string.IsNullOrWhiteSpace(chaveOperacao))
            throw new ArgumentException("Chave da operacao e obrigatoria.", nameof(chaveOperacao));

        var lista = itens.ToList();
        if (lista.Count == 0)
            throw new ArgumentException("Reserva deve conter itens.", nameof(itens));

        ChaveOperacao = chaveOperacao.Trim();
        Status = StatusReservaEstoque.Reservada;
        DataCriacao = DateTimeOffset.UtcNow;
        _itens.AddRange(lista.OrderBy(x => x.TipoMaterial).ThenBy(x => x.MaterialId));
    }

    public string ChaveOperacao { get; private set; } = string.Empty;
    public StatusReservaEstoque Status { get; private set; }
    public DateTimeOffset DataCriacao { get; private set; }
    public DateTimeOffset? DataLiberacao { get; private set; }
    public IReadOnlyCollection<ItemReservaEstoque> Itens => _itens;

    public bool Liberar()
    {
        if (Status == StatusReservaEstoque.Liberada)
            return false;

        Status = StatusReservaEstoque.Liberada;
        DataLiberacao = DateTimeOffset.UtcNow;
        return true;
    }
}

public class ItemReservaEstoque : Entidade
{
    private ItemReservaEstoque() { }

    public ItemReservaEstoque(TipoMaterial tipoMaterial, Guid materialId, int quantidade)
    {
        if (materialId == Guid.Empty)
            throw new ArgumentException("Material invalido.", nameof(materialId));
        if (quantidade <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantidade));

        TipoMaterial = tipoMaterial;
        MaterialId = materialId;
        Quantidade = quantidade;
    }

    public TipoMaterial TipoMaterial { get; private set; }
    public Guid MaterialId { get; private set; }
    public int Quantidade { get; private set; }
    public Guid ReservaEstoqueId { get; private set; }
}
