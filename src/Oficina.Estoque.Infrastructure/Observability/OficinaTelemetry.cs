using System.Diagnostics;

namespace Oficina.Estoque.Infrastructure.Observability;

/// <summary>
/// Fonte unica de spans manuais do servico.
/// O span de envio ao SQS e criado pela instrumentacao AWS, que tambem injeta a
/// propagacao nos MessageAttributes. Aqui ficam apenas os spans que a
/// instrumentacao nao cobre: o despacho do Outbox e o consumo do Inbox.
/// O Meter de negocio existe somente no microsservico de Ordens.
/// </summary>
public static class OficinaTelemetry
{
    public const string ActivitySourceName = "Oficina.Estoque";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    public const string OutboxDispatchActivity = "oficina.outbox.dispatch";
    public const string InboxConsumeActivity = "oficina.inbox.consume";

    public static class Attributes
    {
        public const string CorrelationId = "correlationId";
        public const string CausationId = "causationId";
        public const string OrdemId = "oficina.ordem.id";
        public const string ProcessingResult = "oficina.processing.result";
        public const string MessagingSystem = "messaging.system";
        public const string MessageId = "messaging.message.id";
        public const string MessageType = "messaging.message.type";
        public const string DestinationName = "messaging.destination.name";
    }

    public static class MessageAttributeNames
    {
        public const string Traceparent = "traceparent";
        public const string Tracestate = "tracestate";
        public const string CorrelationId = "correlationId";
        public const string CausationId = "causationId";
        public const string OrdemServicoId = "ordemServicoId";
        public const string MessageType = "messageType";
    }
}
