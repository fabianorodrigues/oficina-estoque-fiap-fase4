using FluentValidation;
using Oficina.Estoque.Application.Contracts;

namespace Oficina.Estoque.Application.Validators;

public sealed class CadastrarPecaRequestValidator : AbstractValidator<CadastrarPecaRequest>
{
    public CadastrarPecaRequestValidator()
    {
        RuleFor(x => x.PrecoUnitario).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Descricao).NotEmpty().MaximumLength(200);
    }
}

public sealed class AtualizarPecaRequestValidator : AbstractValidator<AtualizarPecaRequest>
{
    public AtualizarPecaRequestValidator()
    {
        RuleFor(x => x.PrecoUnitario).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Descricao).NotEmpty().MaximumLength(200);
    }
}
