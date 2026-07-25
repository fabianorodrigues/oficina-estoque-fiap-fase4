using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Oficina.Estoque.Api.Security;
using Oficina.Estoque.Application;
using Oficina.Estoque.Application.UseCases;
using Oficina.Estoque.Domain.CatalogoEstoque;
using Oficina.Estoque.Domain.Reservas;
using Oficina.Estoque.Infrastructure;
using Oficina.Estoque.Infrastructure.Persistencia;

namespace Oficina.Estoque.UnitTests;

public class DevelopmentAuthenticationHandlerTests
{
    private static async Task<AuthenticateResult> Autenticar(Action<HttpContext> configurar)
    {
        var handler = new DevelopmentAuthenticationHandler(
            new OptionsMonitorStub(), NullLoggerFactory.Instance, UrlEncoder.Default);

        var scheme = new AuthenticationScheme(
            DevelopmentAuthenticationDefaults.Scheme,
            DevelopmentAuthenticationDefaults.Scheme,
            typeof(DevelopmentAuthenticationHandler));

        var context = new DefaultHttpContext();
        configurar(context);
        await handler.InitializeAsync(scheme, context);
        return await handler.AuthenticateAsync();
    }

    [Fact]
    public async Task Sem_cabecalho_de_papel_nao_ha_resultado()
    {
        var resultado = await Autenticar(_ => { });

        Assert.False(resultado.Succeeded);
        Assert.Null(resultado.Failure);
    }

    [Fact]
    public async Task Papel_desconhecido_deve_falhar()
    {
        var resultado = await Autenticar(ctx => ctx.Request.Headers["X-Dev-Role"] = "Sindico");

        Assert.Equal("Invalid X-Dev-Role.", resultado.Failure?.Message);
    }

    [Theory]
    [InlineData("cliente", "Cliente")]
    [InlineData("FUNCIONARIO", "Funcionario")]
    [InlineData("Admin", "Admin")]
    public async Task Papel_valido_deve_ser_normalizado(string entrada, string esperado)
    {
        var resultado = await Autenticar(ctx => ctx.Request.Headers["X-Dev-Role"] = entrada);

        Assert.True(resultado.Succeeded);
        Assert.Equal(esperado, resultado.Principal!.FindFirstValue(ClaimTypes.Role));
    }

    [Fact]
    public async Task Deve_projetar_cpf_e_identificadores_informados()
    {
        var clienteId = Guid.NewGuid();
        var funcionarioId = Guid.NewGuid();

        var resultado = await Autenticar(ctx =>
        {
            ctx.Request.Headers["X-Dev-Role"] = "Funcionario";
            ctx.Request.Headers["X-Dev-Cpf"] = "12345678901";
            ctx.Request.Headers["X-Dev-ClienteId"] = clienteId.ToString();
            ctx.Request.Headers["X-Dev-FuncionarioId"] = funcionarioId.ToString();
        });

        var principal = resultado.Principal!;
        Assert.Equal("12345678901", principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal(clienteId.ToString("D"), principal.FindFirstValue("clienteId"));
        Assert.Equal(funcionarioId.ToString("D"), principal.FindFirstValue("funcionarioId"));
    }

    [Fact]
    public async Task Sem_cpf_o_identificador_cai_no_padrao()
    {
        var resultado = await Autenticar(ctx => ctx.Request.Headers["X-Dev-Role"] = "Admin");

        Assert.Equal("development-user", resultado.Principal!.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Null(resultado.Principal.FindFirstValue("cpf"));
    }

    [Theory]
    [InlineData("X-Dev-ClienteId")]
    [InlineData("X-Dev-FuncionarioId")]
    public async Task Identificador_invalido_deve_falhar(string cabecalho)
    {
        var resultado = await Autenticar(ctx =>
        {
            ctx.Request.Headers["X-Dev-Role"] = "Funcionario";
            ctx.Request.Headers[cabecalho] = "nao-e-guid";
        });

        Assert.Equal($"Invalid {cabecalho}.", resultado.Failure?.Message);
    }

    private sealed class OptionsMonitorStub : IOptionsMonitor<AuthenticationSchemeOptions>
    {
        public AuthenticationSchemeOptions CurrentValue { get; } = new();
        public AuthenticationSchemeOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<AuthenticationSchemeOptions, string?> listener) => null;
    }
}

public class ComposicaoDoEstoqueTests
{
    private static IConfiguration Configuracao() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:OficinaEstoqueDb"] = "Server=servidor.example.invalid;Database=OficinaEstoqueDb;User Id=estoque_app;TrustServerCertificate=True",
            ["Messaging:Sqs:Enabled"] = "false"
        })
        .Build();

    [Fact]
    public void Todos_os_use_cases_da_aplicacao_devem_ser_resolviveis()
    {
        var services = new ServiceCollection();
        services.AddEstoqueApplication();
        services.AddEstoqueInfrastructure(Configuracao());

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<PecasUseCases>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<InsumosUseCases>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<EstoqueUseCases>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<DisponibilidadeEstoqueUseCase>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ReservasUseCases>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ConsultarMateriaisUseCase>());
    }

    [Fact]
    public void Infraestrutura_deve_registrar_o_contexto_e_o_repositorio()
    {
        var services = new ServiceCollection();
        services.AddEstoqueApplication();
        services.AddEstoqueInfrastructure(Configuracao());

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<EstoqueDbContext>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<Application.Abstractions.IEstoqueRepository>());
    }

    [Fact]
    public void Sem_connection_string_fora_de_producao_deve_usar_o_fallback_local()
    {
        var services = new ServiceCollection();
        services.AddEstoqueApplication();

        var excecao = Record.Exception(() => services.AddEstoqueInfrastructure(
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Messaging:Sqs:Enabled"] = "false" })
                .Build()));

        Assert.Null(excecao);
    }

    [Fact]
    public void Producao_sem_connection_string_deve_reprovar_no_arranque()
    {
        var services = new ServiceCollection();
        services.AddEstoqueApplication();

        var erro = Assert.Throws<InvalidOperationException>(() => services.AddEstoqueInfrastructure(
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ASPNETCORE_ENVIRONMENT"] = "Production",
                    ["Messaging:Sqs:Enabled"] = "false"
                })
                .Build()));

        Assert.Equal("A connection string obrigatoria nao foi configurada.", erro.Message);
    }
}

public class EstoqueItemTests
{
    [Fact]
    public void Deve_recusar_material_vazio()
    {
        Assert.Throws<ArgumentException>(() => new EstoquePeca(Guid.Empty, 10));
    }

    [Fact]
    public void Deve_recusar_quantidade_inicial_negativa()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EstoquePeca(Guid.NewGuid(), -1));
    }

    [Fact]
    public void Ajuste_negativo_alem_do_saldo_deve_ser_recusado()
    {
        var peca = new EstoquePeca(Guid.NewGuid(), 5);

        var erro = Assert.Throws<InvalidOperationException>(() => peca.Ajustar(-6));

        Assert.Equal("Saldo nao pode ficar negativo.", erro.Message);
        Assert.Equal(5, peca.Quantidade);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Reserva_deve_exigir_quantidade_positiva(int quantidade)
    {
        var peca = new EstoquePeca(Guid.NewGuid(), 5);

        Assert.Throws<ArgumentOutOfRangeException>(() => peca.Reservar(quantidade));
    }

    [Fact]
    public void Reserva_acima_do_saldo_deve_ser_recusada()
    {
        var peca = new EstoquePeca(Guid.NewGuid(), 2);

        var erro = Assert.Throws<InvalidOperationException>(() => peca.Reservar(3));

        Assert.Equal("Saldo insuficiente.", erro.Message);
        Assert.Equal(2, peca.Quantidade);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Liberacao_deve_exigir_quantidade_positiva(int quantidade)
    {
        var peca = new EstoquePeca(Guid.NewGuid(), 5);

        Assert.Throws<ArgumentOutOfRangeException>(() => peca.Liberar(quantidade));
    }

    [Fact]
    public void Reserva_e_liberacao_devem_ser_simetricas()
    {
        var peca = new EstoquePeca(Guid.NewGuid(), 10);

        peca.Reservar(4);
        Assert.Equal(6, peca.Quantidade);

        peca.Liberar(4);
        Assert.Equal(10, peca.Quantidade);
    }

    [Fact]
    public void Insumo_deve_expor_o_material_como_insumo()
    {
        var insumoId = Guid.NewGuid();

        var insumo = new EstoqueInsumo(insumoId, 3);

        Assert.Equal(insumoId, insumo.InsumoId);
        Assert.Equal(insumoId, insumo.MaterialId);
    }
}

public class ReservaEstoqueTests
{
    private static ItemReservaEstoque Item(int quantidade = 1)
        => new(TipoMaterial.Peca, Guid.NewGuid(), quantidade);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Deve_exigir_chave_de_operacao(string chave)
    {
        Assert.Throws<ArgumentException>(() => new ReservaEstoque(chave, [Item()]));
    }

    [Fact]
    public void Deve_exigir_ao_menos_um_item()
    {
        Assert.Throws<ArgumentException>(() => new ReservaEstoque("chave-1", []));
    }

    [Fact]
    public void Deve_nascer_reservada_e_ordenar_os_itens()
    {
        var pecaId = Guid.NewGuid();
        var insumoId = Guid.NewGuid();

        var reserva = new ReservaEstoque("  chave-1  ",
        [
            new ItemReservaEstoque(TipoMaterial.Insumo, insumoId, 1),
            new ItemReservaEstoque(TipoMaterial.Peca, pecaId, 2)
        ]);

        Assert.Equal("chave-1", reserva.ChaveOperacao);
        Assert.Equal(StatusReservaEstoque.Reservada, reserva.Status);
        Assert.Null(reserva.DataLiberacao);
        // A ordenacao estavel dos itens mantem a mensagem de reserva
        // deterministica entre execucoes.
        Assert.Equal(TipoMaterial.Peca, reserva.Itens.First().TipoMaterial);
    }

    [Fact]
    public void Liberar_deve_ser_idempotente()
    {
        var reserva = new ReservaEstoque("chave-1", [Item()]);

        Assert.True(reserva.Liberar());
        Assert.Equal(StatusReservaEstoque.Liberada, reserva.Status);
        Assert.NotNull(reserva.DataLiberacao);

        // A segunda liberacao devolve false: e assim que a compensacao
        // reentregue nao devolve saldo duas vezes.
        Assert.False(reserva.Liberar());
    }

    [Fact]
    public void Item_deve_recusar_material_vazio_e_quantidade_nao_positiva()
    {
        Assert.Throws<ArgumentException>(() => new ItemReservaEstoque(TipoMaterial.Peca, Guid.Empty, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ItemReservaEstoque(TipoMaterial.Peca, Guid.NewGuid(), 0));
    }
}
