using Amazon;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Oficina.Estoque.Infrastructure.Messaging;

public static class SqsMessagingRegistration
{
    public static IServiceCollection AddEstoqueMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SqsMessagingOptions>(configuration.GetSection("Messaging:Sqs"));
        var options = configuration.GetSection("Messaging:Sqs").Get<SqsMessagingOptions>() ?? new();
        if (!options.Enabled)
            return services;

        var isProduction = string.Equals(configuration["ASPNETCORE_ENVIRONMENT"], "Production", StringComparison.OrdinalIgnoreCase);
        ValidateOptions(options, isProduction);

        services.AddSingleton<IAmazonSQS>(_ =>
        {
            var config = new AmazonSQSConfig
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region)
            };
            if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
                config.ServiceURL = options.ServiceUrl;

            return string.IsNullOrWhiteSpace(options.ServiceUrl)
                ? new AmazonSQSClient(config)
                : new AmazonSQSClient(options.AccessKey, options.SecretKey, config);
        });
        services.AddHostedService<EstoqueSqsReceiver>();
        services.AddHostedService<EstoqueInboxProcessor>();
        services.AddHostedService<EstoqueOutboxDispatcher>();
        return services;
    }

    private static void ValidateOptions(SqsMessagingOptions options, bool isProduction)
    {
        if (string.IsNullOrWhiteSpace(options.Region))
            throw new InvalidOperationException("A regiao AWS nao foi configurada.");
        if (options.ConsumerConcurrency != 1)
            throw new InvalidOperationException("A concorrencia do consumer deve ser igual a 1.");
        if (options.MaxMessages != 1)
            throw new InvalidOperationException("O receive SQS deve consumir no maximo uma mensagem.");
        if (options.WaitTimeSeconds is < 1 or > 20)
            throw new InvalidOperationException("O long polling SQS deve estar entre 1 e 20 segundos.");
        if (options.VisibilityTimeoutSeconds is < 1)
            throw new InvalidOperationException("O visibility timeout SQS deve ser positivo.");

        if (!isProduction)
            return;

        if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
            throw new InvalidOperationException("Production nao pode usar endpoint LocalStack.");
        if (string.IsNullOrWhiteSpace(options.CommandsQueueUrl))
            throw new InvalidOperationException("A URL da fila de comandos nao foi configurada.");
        if (string.IsNullOrWhiteSpace(options.EventsQueueUrl))
            throw new InvalidOperationException("A URL da fila de eventos nao foi configurada.");
        if (string.IsNullOrWhiteSpace(options.CommandsDlqQueueUrl))
            throw new InvalidOperationException("A URL da DLQ de comandos nao foi configurada.");
        if (string.IsNullOrWhiteSpace(options.EventsDlqQueueUrl))
            throw new InvalidOperationException("A URL da DLQ de eventos nao foi configurada.");
    }
}
