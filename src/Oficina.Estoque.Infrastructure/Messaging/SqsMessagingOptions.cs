namespace Oficina.Estoque.Infrastructure.Messaging;

public sealed class SqsMessagingOptions
{
    public bool Enabled { get; set; }
    public string ServiceUrl { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
    public string AccessKey { get; set; } = "test";
    public string SecretKey { get; set; } = "test";
    public string CommandsQueueName { get; set; } = "oficina-estoque-comandos.fifo";
    public string CommandsDlqQueueName { get; set; } = "oficina-estoque-comandos-dlq.fifo";
    public string EventsQueueName { get; set; } = "oficina-ordens-eventos.fifo";
}
