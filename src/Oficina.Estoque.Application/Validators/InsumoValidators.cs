using FluentValidation;
using Oficina.Estoque.Application.Contracts;

namespace Oficina.Estoque.Application.Validators;

public sealed class CadastrarInsumoRequestValidator : AbstractValidator<CadastrarInsumoRequest>
{
    public CadastrarInsumoRequestValidator()
    {
        RuleFor(x => x.PrecoUnitario).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Descricao).NotEmpty().MaximumLength(200);
    }
}

public sealed class AtualizarInsumoRequestValidator : AbstractValidator<AtualizarInsumoRequest>
{
    public AtualizarInsumoRequestValidator()
    {
        RuleFor(x => x.PrecoUnitario).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Descricao).NotEmpty().MaximumLength(200);
    }
}
