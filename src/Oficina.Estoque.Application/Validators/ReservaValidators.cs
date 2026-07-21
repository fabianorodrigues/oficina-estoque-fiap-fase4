using FluentValidation;
using Oficina.Estoque.Application.Contracts;

namespace Oficina.Estoque.Application.Validators;

public sealed class DisponibilidadeEstoqueRequestValidator : AbstractValidator<DisponibilidadeEstoqueRequest>
{
    public DisponibilidadeEstoqueRequestValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.MaterialId).NotEmpty();
            item.RuleFor(x => x.RequestedQuantity).GreaterThan(0);
            item.RuleFor(x => x.TipoMaterial).IsInEnum();
        });
    }
}

public sealed class ReservarEstoqueRequestValidator : AbstractValidator<ReservarEstoqueRequest>
{
    public ReservarEstoqueRequestValidator()
    {
        RuleFor(x => x.ChaveOperacao).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Itens).NotEmpty();
        RuleForEach(x => x.Itens).ChildRules(item =>
        {
            item.RuleFor(x => x.TipoMaterial).IsInEnum();
            item.RuleFor(x => x.MaterialId).NotEmpty();
            item.RuleFor(x => x.Quantidade).GreaterThan(0);
        });
    }
}
