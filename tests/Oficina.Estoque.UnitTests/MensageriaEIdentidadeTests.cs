using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Oficina.Estoque.Api.Middleware;
using Oficina.Estoque.Api.Observability;
using Oficina.Estoque.Api.Security;
using Oficina.Estoque.Infrastructure.Messaging;

namespace Oficina.Estoque.UnitTests;

public class MessageJsonTests
{
    private static string EnvelopeValido(Guid ordemServicoId)
        => MessageJson.Envelope(
            EstoqueMessageTypes.ReservarEstoque,
            ordemServicoId,
            "correlacao-1",
            causationId: null,
            payload: new ReservarEstoquePayload("chave-1", [new ReservarEstoqueItemPayload(1, Guid.NewGuid(), 2)]));

    [Fact]
    public void Envelope_deve_serializar_os_campos_do_contrato()
    {
        var ordemServicoId = Guid.NewGuid();

        var envelope = MessageJson.ParseAndValidate(EnvelopeValido(ordemServicoId));

        Assert.NotEqual(Guid.Empty, envelope.MessageId);
        Assert.Equal(EstoqueMessageTypes.ReservarEstoque, envelope.MessageType);
        Assert.Equal(1, envelope.SchemaVersion);
        Assert.Equal(ordemServicoId, envelope.OrdemServicoId);
        Assert.Equal("correlacao-1", envelope.CorrelationId);
        Assert.Null(envelope.CausationId);
    }

    [Fact]
    public void Envelope_deve_gerar_message_id_distinto_a_cada_chamada()
    {
        var ordemServicoId = Guid.NewGuid();

        var primeiro = MessageJson.ParseAndValidate(EnvelopeValido(ordemServicoId));
        var segundo = MessageJson.ParseAndValidate(EnvelopeValido(ordemServicoId));

        Assert.NotEqual(primeiro.MessageId, segundo.MessageId);
    }

    [Fact]
    public void Payload_deve_sobreviver_ao_round_trip()
    {
        var materialId = Guid.NewGuid();
        var body = MessageJson.Envelope(
            EstoqueMessageTypes.ReservarEstoque, Guid.NewGuid(), "correlacao-1", null,
            new ReservarEstoquePayload("chave-1", [new ReservarEstoqueItemPayload(1, materialId, 3)]));

        var payload = MessageJson.ParseAndValidate(body).Payload.Deserialize<ReservarEstoquePayload>(MessageJson.Options)!;

        Assert.Equal("chave-1", payload.ChaveOperacao);
        Assert.Equal(materialId, payload.Itens[0].MaterialId);
        Assert.Equal(3, payload.Itens[0].Quantidade);
    }

    [Fact]
    public void Deve_aceitar_propriedades_com_caixa_diferente()
    {
        // As mensagens vem de outro servico: a leitura nao pode depender da
        // caixa exata das propriedades.
        var body = """
            {"MessageId":"11111111-1111-1111-1111-111111111111","MessageType":"ReservarEstoque",
             "SchemaVersion":1,"OccurredAtUtc":"2026-01-01T00:00:00Z","CorrelationId":"c1",
             "CausationId":null,"OrdemServicoId":"22222222-2222-2222-2222-222222222222","Payload":{}}
            """;

        var envelope = MessageJson.ParseAndValidate(body);

        Assert.Equal("ReservarEstoque", envelope.MessageType);
    }

    [Theory]
    [InlineData("null", "Envelope ausente.")]
    public void Deve_recusar_envelope_nulo(string body, string mensagem)
    {
        var erro = Assert.Throws<InvalidOperationException>(() => MessageJson.ParseAndValidate(body));
        Assert.Equal(mensagem, erro.Message);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", "ReservarEstoque", 1, "22222222-2222-2222-2222-222222222222", "c1", "MessageId invalido.")]
    [InlineData("11111111-1111-1111-1111-111111111111", "", 1, "22222222-2222-2222-2222-222222222222", "c1", "MessageType invalido.")]
    [InlineData("11111111-1111-1111-1111-111111111111", "ReservarEstoque", 2, "22222222-2222-2222-2222-222222222222", "c1", "SchemaVersion invalida.")]
    [InlineData("11111111-1111-1111-1111-111111111111", "ReservarEstoque", 1, "00000000-0000-0000-0000-000000000000", "c1", "OrdemServicoId invalido.")]
    [InlineData("11111111-1111-1111-1111-111111111111", "ReservarEstoque", 1, "22222222-2222-2222-2222-222222222222", "", "CorrelationId invalido.")]
    public void Deve_recusar_envelope_fora_do_contrato(
        string messageId, string messageType, int schemaVersion, string ordemServicoId, string correlationId, string mensagem)
    {
        var body = JsonSerializer.Serialize(new
        {
            messageId,
            messageType,
            schemaVersion,
            occurredAtUtc = DateTimeOffset.UtcNow,
            correlationId,
            causationId = (string?)null,
            ordemServicoId,
            payload = new { }
        });

        var erro = Assert.Throws<InvalidOperationException>(() => MessageJson.ParseAndValidate(body));
        Assert.Equal(mensagem, erro.Message);
    }
}

public class InboxMessageTests
{
    private static InboxMessage Nova() => new(
        Guid.NewGuid(), EstoqueMessageTypes.ReservarEstoque, Guid.NewGuid(), "correlacao-1", "{}");

    [Fact]
    public void Deve_nascer_recebida_sem_tentativas()
    {
        var inbox = Nova();

        Assert.Equal(InboxMessageStatus.Received, inbox.Status);
        Assert.Equal(0, inbox.Attempts);
        Assert.Null(inbox.LockedUntilUtc);
        Assert.Null(inbox.ProcessedAtUtc);
        Assert.Null(inbox.Error);
    }

    [Fact]
    public void Claim_deve_travar_a_mensagem_e_contar_a_tentativa()
    {
        var inbox = Nova();
        var ate = DateTimeOffset.UtcNow.AddMinutes(1);

        inbox.Claim(ate);
        inbox.Claim(ate);

        Assert.Equal(InboxMessageStatus.Processing, inbox.Status);
        Assert.Equal(2, inbox.Attempts);
        Assert.Equal(ate, inbox.LockedUntilUtc);
    }

    [Fact]
    public void MarkProcessed_deve_limpar_lock_e_erro()
    {
        var inbox = Nova();
        inbox.Claim(DateTimeOffset.UtcNow.AddMinutes(1));
        inbox.MarkDeferred("fora de ordem");

        inbox.MarkProcessed();

        Assert.Equal(InboxMessageStatus.Processed, inbox.Status);
        Assert.NotNull(inbox.ProcessedAtUtc);
        Assert.Null(inbox.LockedUntilUtc);
        Assert.Null(inbox.Error);
    }

    [Fact]
    public void MarkDeferred_deve_reagendar_a_mensagem_fora_de_ordem()
    {
        var inbox = Nova();

        inbox.MarkDeferred("reserva ainda nao existe");

        Assert.Equal(InboxMessageStatus.Deferred, inbox.Status);
        Assert.NotNull(inbox.LockedUntilUtc);
        Assert.True(inbox.LockedUntilUtc > DateTimeOffset.UtcNow);
        Assert.Equal("reserva ainda nao existe", inbox.Error);
    }

    [Theory]
    [InlineData(true, InboxMessageStatus.DeadLettered)]
    [InlineData(false, InboxMessageStatus.Received)]
    public void MarkFailed_deve_escolher_entre_dlq_e_nova_tentativa(bool deadLetter, InboxMessageStatus esperado)
    {
        var inbox = Nova();

        inbox.MarkFailed("falha de processamento", deadLetter);

        Assert.Equal(esperado, inbox.Status);
        Assert.Null(inbox.LockedUntilUtc);
    }

    [Fact]
    public void Erro_deve_ser_truncado_em_500_caracteres()
    {
        // A coluna Error tem limite de 500: truncar aqui evita que uma excecao
        // longa derrube a gravacao no banco.
        var inbox = Nova();

        inbox.MarkFailed(new string('x', 900), deadLetter: false);

        Assert.Equal(500, inbox.Error!.Length);
    }
}

public class OutboxMessageTests
{
    private static OutboxMessage Nova() => new(
        Guid.NewGuid(), EstoqueMessageTypes.EstoqueReservado, Guid.NewGuid(), "correlacao-1", "causa-1", "{}");

    [Fact]
    public void Deve_nascer_nao_publicada()
    {
        var outbox = Nova();

        Assert.Null(outbox.PublishedAtUtc);
        Assert.Equal(0, outbox.Attempts);
        Assert.Equal("causa-1", outbox.CausationId);
    }

    [Fact]
    public void Claim_deve_contar_tentativa_e_travar()
    {
        var outbox = Nova();
        var ate = DateTimeOffset.UtcNow.AddMinutes(1);

        outbox.Claim(ate);

        Assert.Equal(1, outbox.Attempts);
        Assert.Equal(ate, outbox.LockedUntilUtc);
    }

    [Fact]
    public void MarkPublished_deve_liberar_o_lock_e_limpar_o_erro()
    {
        var outbox = Nova();
        outbox.Claim(DateTimeOffset.UtcNow.AddMinutes(1));
        outbox.MarkFailed("timeout do SQS");

        outbox.MarkPublished();

        Assert.NotNull(outbox.PublishedAtUtc);
        Assert.Null(outbox.LockedUntilUtc);
        Assert.Null(outbox.Error);
    }

    [Fact]
    public void MarkFailed_deve_liberar_o_lock_para_nova_tentativa()
    {
        var outbox = Nova();
        outbox.Claim(DateTimeOffset.UtcNow.AddMinutes(1));

        outbox.MarkFailed(new string('y', 900));

        Assert.Null(outbox.LockedUntilUtc);
        Assert.Equal(500, outbox.Error!.Length);
        Assert.Null(outbox.PublishedAtUtc);
    }
}

public class SqsMessagingRegistrationTests
{
    private static IConfiguration Configuracao(Dictionary<string, string?> valores)
        => new ConfigurationBuilder().AddInMemoryCollection(valores).Build();

    private static Dictionary<string, string?> Base(bool producao) => new()
    {
        ["ASPNETCORE_ENVIRONMENT"] = producao ? "Production" : "Development",
        ["Messaging:Sqs:Enabled"] = "true",
        ["Messaging:Sqs:Region"] = "us-east-1",
        ["Messaging:Sqs:ConsumerConcurrency"] = "1",
        ["Messaging:Sqs:MaxMessages"] = "1",
        ["Messaging:Sqs:WaitTimeSeconds"] = "20",
        ["Messaging:Sqs:VisibilityTimeoutSeconds"] = "60",
        ["Messaging:Sqs:CommandsQueueUrl"] = "https://sqs.example.invalid/comandos",
        ["Messaging:Sqs:CommandsDlqQueueUrl"] = "https://sqs.example.invalid/comandos-dlq",
        ["Messaging:Sqs:EventsQueueUrl"] = "https://sqs.example.invalid/eventos",
        ["Messaging:Sqs:EventsDlqQueueUrl"] = "https://sqs.example.invalid/eventos-dlq"
    };

    [Fact]
    public void Deve_ignorar_o_registro_quando_a_mensageria_esta_desabilitada()
    {
        var services = new ServiceCollection();

        services.AddEstoqueMessaging(Configuracao(new Dictionary<string, string?>
        {
            ["Messaging:Sqs:Enabled"] = "false"
        }));

        // Sem consumidores registrados o servico sobe sem tocar no SQS: e assim
        // que a mensageria fica opcional no ambiente local.
        Assert.DoesNotContain(services, x => x.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService));
    }

    [Fact]
    public void Deve_registrar_os_tres_workers_quando_habilitada()
    {
        var services = new ServiceCollection();

        services.AddEstoqueMessaging(Configuracao(Base(producao: false)));

        Assert.Equal(3, services.Count(x => x.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)));
        Assert.Contains(services, x => x.ServiceType == typeof(Amazon.SQS.IAmazonSQS));
    }

    [Theory]
    [InlineData("Messaging:Sqs:Region", "", "A regiao AWS nao foi configurada.")]
    [InlineData("Messaging:Sqs:ConsumerConcurrency", "2", "A concorrencia do consumer deve ser igual a 1.")]
    [InlineData("Messaging:Sqs:MaxMessages", "10", "O receive SQS deve consumir no maximo uma mensagem.")]
    [InlineData("Messaging:Sqs:WaitTimeSeconds", "0", "O long polling SQS deve estar entre 1 e 20 segundos.")]
    [InlineData("Messaging:Sqs:WaitTimeSeconds", "21", "O long polling SQS deve estar entre 1 e 20 segundos.")]
    [InlineData("Messaging:Sqs:VisibilityTimeoutSeconds", "0", "O visibility timeout SQS deve ser positivo.")]
    public void Deve_reprovar_configuracao_invalida(string chave, string valor, string mensagem)
    {
        var valores = Base(producao: false);
        valores[chave] = valor;

        var erro = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddEstoqueMessaging(Configuracao(valores)));

        Assert.Equal(mensagem, erro.Message);
    }

    [Fact]
    public void Producao_nao_pode_apontar_para_endpoint_local()
    {
        var valores = Base(producao: true);
        valores["Messaging:Sqs:ServiceUrl"] = "http://localstack:4566";

        var erro = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddEstoqueMessaging(Configuracao(valores)));

        Assert.Equal("Production nao pode usar endpoint LocalStack.", erro.Message);
    }

    [Theory]
    [InlineData("Messaging:Sqs:CommandsQueueUrl", "A URL da fila de comandos nao foi configurada.")]
    [InlineData("Messaging:Sqs:EventsQueueUrl", "A URL da fila de eventos nao foi configurada.")]
    [InlineData("Messaging:Sqs:CommandsDlqQueueUrl", "A URL da DLQ de comandos nao foi configurada.")]
    [InlineData("Messaging:Sqs:EventsDlqQueueUrl", "A URL da DLQ de eventos nao foi configurada.")]
    public void Producao_exige_todas_as_urls_de_fila(string chave, string mensagem)
    {
        var valores = Base(producao: true);
        valores[chave] = "";

        var erro = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddEstoqueMessaging(Configuracao(valores)));

        Assert.Equal(mensagem, erro.Message);
    }

    [Fact]
    public void Endpoint_local_deve_registrar_cliente_com_credenciais_explicitas()
    {
        var valores = Base(producao: false);
        valores["Messaging:Sqs:ServiceUrl"] = "http://localstack:4566";
        var services = new ServiceCollection();

        services.AddEstoqueMessaging(Configuracao(valores));

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<Amazon.SQS.IAmazonSQS>());
    }
}

public class TrustedIdentityAuthenticationHandlerTests
{
    private static async Task<AuthenticateResult> Autenticar(Action<HttpContext> configurar)
    {
        var handler = new TrustedIdentityAuthenticationHandler(
            new OptionsMonitorStub(), NullLoggerFactory.Instance, UrlEncoder.Default);

        var scheme = new AuthenticationScheme(
            TrustedIdentityAuthenticationDefaults.Scheme,
            TrustedIdentityAuthenticationDefaults.Scheme,
            typeof(TrustedIdentityAuthenticationHandler));

        var context = new DefaultHttpContext();
        configurar(context);
        await handler.InitializeAsync(scheme, context);
        return await handler.AuthenticateAsync();
    }

    [Fact]
    public async Task Sem_cabecalhos_de_identidade_nao_ha_resultado()
    {
        var resultado = await Autenticar(_ => { });

        Assert.False(resultado.Succeeded);
        Assert.Null(resultado.Failure);
    }

    [Fact]
    public async Task Deve_falhar_quando_ha_papel_mas_falta_identificador()
    {
        var resultado = await Autenticar(ctx =>
            ctx.Request.Headers[TrustedIdentityAuthenticationDefaults.UserRoleHeader] = "Funcionario");

        Assert.Equal("Identidade sem identificador.", resultado.Failure?.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Sindico")]
    public async Task Deve_falhar_quando_o_perfil_e_invalido(string papel)
    {
        var resultado = await Autenticar(ctx =>
        {
            ctx.Request.Headers[TrustedIdentityAuthenticationDefaults.UserIdHeader] = Guid.NewGuid().ToString();
            ctx.Request.Headers[TrustedIdentityAuthenticationDefaults.UserRoleHeader] = papel;
        });

        Assert.Equal("Identidade com perfil invalido.", resultado.Failure?.Message);
    }

    [Fact]
    public async Task Cliente_com_identificador_guid_recebe_claim_de_cliente()
    {
        var id = Guid.NewGuid();

        var resultado = await Autenticar(ctx =>
        {
            ctx.Request.Headers[TrustedIdentityAuthenticationDefaults.UserIdHeader] = id.ToString();
            ctx.Request.Headers[TrustedIdentityAuthenticationDefaults.UserRoleHeader] = "cliente";
            ctx.Request.Headers[TrustedIdentityAuthenticationDefaults.UserCpfHeader] = "12345678901";
            ctx.Request.Headers[TrustedIdentityAuthenticationDefaults.UserNameHeader] = "Maria";
        });

        Assert.True(resultado.Succeeded);
        var principal = resultado.Principal!;
        Assert.Equal("Cliente", principal.FindFirstValue(ClaimTypes.Role));
        Assert.Equal(id.ToString("D"), principal.FindFirstValue("clienteId"));
        Assert.Null(principal.FindFirstValue("funcionarioId"));
        Assert.Equal("12345678901", principal.FindFirstValue("cpf"));
        Assert.Equal("Maria", principal.FindFirstValue(ClaimTypes.Name));
    }

    [Fact]
    public async Task Funcionario_com_identificador_guid_recebe_claim_de_funcionario()
    {
        var id = Guid.NewGuid();

        var resultado = await Autenticar(ctx =>
        {
            ctx.Request.Headers[TrustedIdentityAuthenticationDefaults.UserIdHeader] = id.ToString();
            ctx.Request.Headers[TrustedIdentityAuthenticationDefaults.UserRoleHeader] = "Funcionario";
        });

        Assert.Equal(id.ToString("D"), resultado.Principal!.FindFirstValue("funcionarioId"));
        Assert.Null(resultado.Principal.FindFirstValue("cpf"));
    }

    [Fact]
    public async Task Identificador_que_nao_e_guid_nao_gera_claim_derivada()
    {
        var resultado = await Autenticar(ctx =>
        {
            ctx.Request.Headers[TrustedIdentityAuthenticationDefaults.UserIdHeader] = "usuario-externo";
            ctx.Request.Headers[TrustedIdentityAuthenticationDefaults.UserRoleHeader] = "Admin";
        });

        Assert.True(resultado.Succeeded);
        Assert.Equal("usuario-externo", resultado.Principal!.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Null(resultado.Principal.FindFirstValue("clienteId"));
        Assert.Null(resultado.Principal.FindFirstValue("funcionarioId"));
    }

    private sealed class OptionsMonitorStub : IOptionsMonitor<AuthenticationSchemeOptions>
    {
        public AuthenticationSchemeOptions CurrentValue { get; } = new();
        public AuthenticationSchemeOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<AuthenticationSchemeOptions, string?> listener) => null;
    }
}

public class ObservabilidadeECorrelacaoTests
{
    [Fact]
    public void Telemetria_desabilitada_nao_registra_nada()
    {
        var services = new ServiceCollection();

        services.AddOpenTelemetryFailOpen(
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["OpenTelemetry:Enabled"] = "false" })
                .Build(),
            new LoggingBuilderStub(services),
            "oficina-estoque");

        Assert.DoesNotContain(services, x => x.ServiceType.FullName?.Contains("OpenTelemetry") == true);
    }

    [Fact]
    public void Telemetria_habilitada_registra_tracing()
    {
        var services = new ServiceCollection();

        services.AddOpenTelemetryFailOpen(
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["OpenTelemetry:Enabled"] = "true",
                    ["OpenTelemetry:OtlpEndpoint"] = "http://collector.example.invalid:4317"
                })
                .Build(),
            new LoggingBuilderStub(services),
            "oficina-estoque");

        Assert.Contains(services, x => x.ServiceType.FullName?.Contains("OpenTelemetry") == true);
    }

    [Fact]
    public void Falha_na_telemetria_nao_pode_derrubar_a_aplicacao()
    {
        var services = new ServiceCollection();

        var excecao = Record.Exception(() => services.AddOpenTelemetryFailOpen(
            new ConfiguracaoQueFalha(), new LoggingBuilderStub(services), "oficina-estoque"));

        Assert.Null(excecao);
    }

    [Fact]
    public async Task Correlation_id_recebido_deve_ser_preservado_na_resposta()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "correlacao-externa";
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask, NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.Invoke(context);

        Assert.Equal("correlacao-externa", context.Response.Headers[CorrelationIdMiddleware.HeaderName]);
    }

    [Fact]
    public async Task Correlation_id_ausente_deve_ser_gerado()
    {
        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask, NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.Invoke(context);

        Assert.True(Guid.TryParse(context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString(), out _));
    }

    private sealed class ConfiguracaoQueFalha : IConfiguration
    {
        public string? this[string key]
        {
            get => throw new InvalidOperationException("Provedor indisponivel.");
            set => throw new InvalidOperationException("Provedor indisponivel.");
        }

        public IEnumerable<IConfigurationSection> GetChildren() => throw new InvalidOperationException();
        public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken() => throw new InvalidOperationException();
        public IConfigurationSection GetSection(string key) => throw new InvalidOperationException();
    }

    private sealed class LoggingBuilderStub(IServiceCollection services) : ILoggingBuilder
    {
        public IServiceCollection Services { get; } = services;
    }
}
