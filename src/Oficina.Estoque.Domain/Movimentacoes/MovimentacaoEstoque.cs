using Oficina.Estoque.Domain.CatalogoEstoque;
using Oficina.Estoque.Domain.SharedKernel;

namespace Oficina.Estoque.Domain.Movimentacoes;

public enum TipoMovimentacaoEstoque
{
    Entrada = 1,
    Saida = 2,
    Reserva = 3,
    Liberacao = 4
}

public class MovimentacaoEstoque : Entidade
{
    private MovimentacaoEstoque() { }

    public MovimentacaoEstoque(
        TipoMaterial tipoMaterial,
        Guid materialId,
        TipoMovimentacaoEstoque tipo,
        int quantidade,
        int saldoResultante,
        string referenciaOperacao)
    {
        if (materialId == Guid.Empty)
            throw new ArgumentException("Material invalido.", nameof(materialId));
        if (quantidade <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantidade));
        if (string.IsNullOrWhiteSpace(referenciaOperacao))
            throw new ArgumentException("Referencia da operacao e obrigatoria.", nameof(referenciaOperacao));

        TipoMaterial = tipoMaterial;
        MaterialId = materialId;
        Tipo = tipo;
        Quantidade = quantidade;
        SaldoResultante = saldoResultante;
        ReferenciaOperacao = referenciaOperacao.Trim();
        Data = DateTimeOffset.UtcNow;
    }

    public TipoMaterial TipoMaterial { get; private set; }
    public Guid MaterialId { get; private set; }
    public TipoMovimentacaoEstoque Tipo { get; private set; }
    public int Quantidade { get; private set; }
    public int SaldoResultante { get; private set; }
    public DateTimeOffset Data { get; private set; }
    public string ReferenciaOperacao { get; private set; } = string.Empty;
}
