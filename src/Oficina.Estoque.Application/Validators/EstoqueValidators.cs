using FluentValidation;
using Oficina.Estoque.Application.Contracts;

namespace Oficina.Estoque.Application.Validators;

public sealed class AjustarEstoqueRequestValidator : AbstractValidator<AjustarEstoqueRequest>
{
    public AjustarEstoqueRequestValidator() => RuleFor(x => x.Quantidade).NotEqual(0);
}
