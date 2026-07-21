using FluentValidation;
using Oficina.Estoque.Application.Abstractions;
using Oficina.Estoque.Application.Contracts;
using Oficina.Estoque.Application.Shared;
using Oficina.Estoque.Domain.CatalogoEstoque;
using Oficina.Estoque.Domain.Movimentacoes;
using Oficina.Estoque.Domain.Reservas;

namespace Oficina.Estoque.Application.UseCases;

public sealed class ReservasUseCases(IEstoqueRepository repository, IValidator<ReservarEstoqueRequest> validator)
{
    public async Task<ReservarEstoqueResponse> ReservarAsync(ReservarEstoqueRequest request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);

        var existente = await repository.ObterReservaPorChaveAsync(request.ChaveOperacao, ct);
        if (existente is not null)
            return new ReservarEstoqueResponse(existente.Id, true);

        var itens = request.Itens
            .OrderBy(x => x.TipoMaterial)
            .ThenBy(x => x.MaterialId)
            .ToList();

        var saldos = new List<(ReservarEstoqueItemRequest Item, EstoqueItem Estoque)>();
        foreach (var item in itens)
        {
            var estoque = await repository.ObterEstoqueItemAsync(item.TipoMaterial, item.MaterialId, ct);
            if (estoque is null)
                throw EstoqueException.Conflict("Material inexistente recusa toda a reserva.");
            if (estoque.Quantidade < item.Quantidade)
                throw EstoqueException.Conflict("Saldo insuficiente recusa toda a reserva.");

            saldos.Add((item, estoque));
        }

        foreach (var (item, estoque) in saldos)
        {
            estoque.Reservar(item.Quantidade);
            repository.AdicionarMovimentacao(new MovimentacaoEstoque(
                item.TipoMaterial,
                item.MaterialId,
                TipoMovimentacaoEstoque.Reserva,
                item.Quantidade,
                estoque.Quantidade,
                request.ChaveOperacao));
        }

        var reserva = new ReservaEstoque(
            request.ChaveOperacao,
            itens.Select(x => new ItemReservaEstoque(x.TipoMaterial, x.MaterialId, x.Quantidade)));
        repository.AdicionarReserva(reserva);
        await repository.SalvarAsync(ct);

        return new ReservarEstoqueResponse(reserva.Id, false);
    }

    public async Task<LiberarReservaEstoqueResponse> LiberarAsync(Guid reservaId, CancellationToken ct)
    {
        var reserva = await repository.ObterReservaAsync(reservaId, ct)
            ?? throw EstoqueException.NotFound("Reserva nao encontrada.");

        if (!reserva.Liberar())
            return new LiberarReservaEstoqueResponse(reserva.Id, true);

        foreach (var item in reserva.Itens.OrderBy(x => x.TipoMaterial).ThenBy(x => x.MaterialId))
        {
            var estoque = await repository.ObterEstoqueItemAsync(item.TipoMaterial, item.MaterialId, ct)
                ?? throw EstoqueException.Conflict("Material da reserva nao encontrado no estoque.");
            estoque.Liberar(item.Quantidade);
            repository.AdicionarMovimentacao(new MovimentacaoEstoque(
                item.TipoMaterial,
                item.MaterialId,
                TipoMovimentacaoEstoque.Liberacao,
                item.Quantidade,
                estoque.Quantidade,
                reserva.ChaveOperacao));
        }

        await repository.SalvarAsync(ct);
        return new LiberarReservaEstoqueResponse(reserva.Id, false);
    }
}
