using Oficina.Estoque.Domain.SharedKernel;

namespace Oficina.Estoque.Domain.CatalogoEstoque;

public class Peca : AgregadoRaiz
{
    private Peca() { }

    public Peca(decimal precoUnitario, string descricao)
    {
        DefinirDescricao(descricao);
        DefinirPreco(precoUnitario);
    }

    public string Descricao { get; private set; } = string.Empty;
    public decimal PrecoUnitario { get; private set; }

    public void Atualizar(decimal precoUnitario, string descricao)
    {
        DefinirPreco(precoUnitario);
        DefinirDescricao(descricao);
    }

    public void DefinirDescricao(string descricao)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("Descricao e obrigatoria.", nameof(descricao));

        Descricao = descricao.Trim();
    }

    public void DefinirPreco(decimal precoUnitario)
    {
        if (precoUnitario < 0)
            throw new ArgumentOutOfRangeException(nameof(precoUnitario));

        PrecoUnitario = precoUnitario;
    }
}
