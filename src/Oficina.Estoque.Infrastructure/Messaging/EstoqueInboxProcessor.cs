using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Oficina.Estoque.Application.Contracts;
using Oficina.Estoque.Application.Shared;
using Oficina.Estoque.Application.UseCases;
using Oficina.Estoque.Domain.CatalogoEstoque;
using Oficina.Estoque.Infrastructure.Persistencia;

namespace Oficina.Estoque.Infrastructure.Messaging;

internal sealed class EstoqueInboxProcessor(
    IServiceScopeFactory scopes,
    IAmazonSQS sqs,
    Microsoft.Extensions.Options.IOptions<SqsMessagingOptions> options,
    ILogger<EstoqueInboxProcessor> logger) : SqsBackgroundService(logger)
{
    protected override async Task ExecuteOnce(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<EstoqueDbContext>();
        var now = DateTimeOffset.UtcNow;
        var inbox = await db.InboxMessages
            .Where(x => x.Status == InboxMessageStatus.Received || (x.Status == InboxMessageStatus.Deferred && x.LockedUntilUtc < now) || (x.Status == InboxMessageStatus.Processing && x.LockedUntilUtc < now))
            .OrderBy(x => x.ReceivedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (inbox is null)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
            return;
        }

        inbox.Claim(now.AddSeconds(30));
        await db.SaveChangesAsync(ct);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var envelope = MessageJson.ParseAndValidate(inbox.Body);
            if (inbox.MessageType == EstoqueMessageTypes.ReservarEstoque)
                await ProcessarReserva(scope.ServiceProvider, db, inbox, envelope, ct);
            else if (inbox.MessageType == EstoqueMessageTypes.LiberarReservaEstoque)
                await ProcessarLiberacao(scope.ServiceProvider, db, inbox, envelope, ct);
            else
                inbox.MarkDeferred("Tipo de comando ainda nao processavel.");

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            logger.LogError(ex, "Falha ao processar Inbox {MessageId}.", inbox.MessageId);
            if (inbox.Attempts >= 3)
            {
                await PublishExplicitDlq(inbox, ct);
                inbox.MarkFailed(ex.Message, deadLetter: true);
            }
            else
            {
                inbox.MarkFailed(ex.Message, deadLetter: false);
            }

            await db.SaveChangesAsync(ct);
        }
    }

    private static async Task ProcessarReserva(IServiceProvider services, EstoqueDbContext db, InboxMessage inbox, MessageEnvelope envelope, CancellationToken ct)
    {
        var payload = envelope.Payload.Deserialize<ReservarEstoquePayload>(MessageJson.Options)
            ?? throw new InvalidOperationException("Payload de reserva invalido.");
        var request = new ReservarEstoqueRequest(
            payload.ChaveOperacao,
            payload.Itens.Select(x => new ReservarEstoqueItemRequest((TipoMaterial)x.TipoMaterial, x.MaterialId, x.Quantidade)).ToList());

        try
        {
            var result = await services.GetRequiredService<ReservasUseCases>().ReservarAsync(request, ct);
            db.OutboxMessages.Add(CreateOutbox(envelope, EstoqueMessageTypes.EstoqueReservado, new EstoqueReservadoPayload(result.ReservaId, result.Duplicada)));
        }
        catch (EstoqueException ex) when (ex.StatusCode is 409 or 404 or 400)
        {
            db.OutboxMessages.Add(CreateOutbox(envelope, EstoqueMessageTypes.ReservaEstoqueRecusada, new ReservaEstoqueRecusadaPayload(ex.Code, ex.Message)));
        }

        inbox.MarkProcessed();
    }

    private static async Task ProcessarLiberacao(IServiceProvider services, EstoqueDbContext db, InboxMessage inbox, MessageEnvelope envelope, CancellationToken ct)
    {
        var payload = envelope.Payload.Deserialize<LiberarReservaEstoquePayload>(MessageJson.Options)
            ?? throw new InvalidOperationException("Payload de liberacao invalido.");
        var reservaId = payload.ReservaId;
        if (reservaId == Guid.Empty && !string.IsNullOrWhiteSpace(payload.ChaveOperacao))
        {
            var reserva = await db.ReservasEstoque.FirstOrDefaultAsync(x => x.ChaveOperacao == payload.ChaveOperacao, ct)
                ?? throw EstoqueException.NotFound("Reserva nao encontrada.");
            reservaId = reserva.Id;
        }

        try
        {
            var result = await services.GetRequiredService<ReservasUseCases>().LiberarAsync(reservaId, ct);
            db.OutboxMessages.Add(CreateOutbox(envelope, EstoqueMessageTypes.ReservaEstoqueLiberada, new ReservaEstoqueLiberadaPayload(result.ReservaId, result.JaLiberada)));
        }
        catch (EstoqueException ex)
        {
            db.OutboxMessages.Add(CreateOutbox(envelope, EstoqueMessageTypes.LiberacaoReservaFalhou, new LiberacaoReservaFalhouPayload(reservaId == Guid.Empty ? null : reservaId, ex.Code, ex.Message)));
        }

        inbox.MarkProcessed();
    }

    private static OutboxMessage CreateOutbox(MessageEnvelope source, string type, object payload)
    {
        var body = MessageJson.Envelope(type, source.OrdemServicoId, source.CorrelationId, source.MessageId.ToString(), payload);
        var envelope = MessageJson.ParseAndValidate(body);
        return new OutboxMessage(envelope.MessageId, type, source.OrdemServicoId, source.CorrelationId, source.MessageId.ToString(), body);
    }

    private async Task PublishExplicitDlq(InboxMessage inbox, CancellationToken ct)
    {
        var dlqUrl = await QueueUrlResolver.Resolve(sqs, options.Value, options.Value.CommandsDlqQueueName, options.Value.CommandsDlqQueueUrl, ct);
        await sqs.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = dlqUrl,
            MessageBody = inbox.Body,
            MessageGroupId = inbox.OrdemServicoId.ToString(),
            MessageDeduplicationId = inbox.MessageId.ToString()
        }, ct);
    }
}
