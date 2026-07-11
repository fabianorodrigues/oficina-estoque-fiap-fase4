# oficina-estoque-fiap-fase4

Microsservico responsavel pelo catalogo de pecas e insumos, saldos de estoque, reservas e movimentacoes da Oficina.

## Arquitetura

Clean Architecture com 4 camadas:

- `Oficina.Estoque.Domain` — entidades e agregados (`CatalogoEstoque`, `Movimentacoes`, `Reservas`); sem dependencias externas.
- `Oficina.Estoque.Application` — casos de uso, contratos (`Contracts/`) e validators.
- `Oficina.Estoque.Infrastructure` — persistencia EF Core (`EstoqueDbContext`), migrations e mensageria SQS (Inbox/Outbox).
- `Oficina.Estoque.Api` — controllers, autenticacao/autorizacao, middlewares e composition root (`Program.cs`).

## Endpoints principais

- `api/pecas` — CRUD de pecas.
- `api/insumos` — CRUD de insumos.
- `api/estoque` — consulta e ajuste de saldos.
- `api/internal/estoque/disponibilidade` e `api/internal/materiais/consulta` — consultas usadas pelo servico de Ordens de Servico.

## Mensageria

Consome comandos (`ReservarEstoque`, `LiberarReservaEstoque`) e publica eventos (`EstoqueReservado`, `ReservaEstoqueRecusada`, `ReservaEstoqueLiberada`, `LiberacaoReservaFalhou`) via filas SQS FIFO, com padrao Inbox/Outbox para idempotencia e ordenacao por `OrdemServicoId`. Habilitado via `Messaging:Sqs:Enabled`.

Autenticacao em ambiente local via header scheme (`Authentication:Mode=Development`), bloqueada fora de `Development`.

Fluxo assincrono:

```text
SQS comandos -> Consumer -> Inbox -> Regra de estoque -> Outbox -> Dispatcher -> SQS eventos
```

## Publicacao oficial

A configuracao oficial nao sensivel fica em `config/official.json`. A etapa de deploy futura usa:

- Banco `OficinaEstoqueDb`.
- Usuario runtime `estoque_app`.
- Usuario migration `estoque_migrator`.
- Secret runtime `/oficina/estoque/runtime-db`.
- Secret migration `/oficina/estoque/migration-db`.
- Fila de comandos `oficina-estoque-comandos.fifo`.
- DLQ de comandos `oficina-estoque-comandos-dlq.fifo`.
- Fila de eventos `oficina-ordens-eventos.fifo`.
- DLQ de eventos `oficina-ordens-eventos-dlq.fifo`.

As senhas e connection strings sao geridas pelo processo centralizado de banco e nao sao configuradas neste repositorio. Em Production, a connection string e montada via Secret Store CSI em `/mnt/secrets-store` e lida por `AddKeyPerFile`.

Decisoes fixas desta etapa:

- Deployment `oficina-estoque` no namespace `oficina`.
- Service `ClusterIP`.
- 1 replica.
- Strategy `Recreate`.
- Sem HPA.
- Consumer concurrency 1.
- Receive SQS com no maximo 1 mensagem e long polling de 20 segundos.
- Inbox para idempotencia.
- Outbox para publicacao confiavel.
- EF Migration Bundle executado por imagem de migration antes do Deployment.

## Development e Production

Em `Development`, a aplicacao preserva SQL Server local, LocalStack quando `Messaging:Sqs:ServiceUrl` for configurado, autenticacao de desenvolvimento e migrations opcionais apenas com `Database:ApplyMigrations=true`.

Em `Production`, a aplicacao exige connection string, regiao AWS, URLs de filas e DLQs. Nao ha fallback para LocalStack nem banco local, e migrations automaticas sao bloqueadas fora de `Development`.

## GitHub Actions

- `Estoque CI`: valida PRs para `main`, restaura, compila, testa, valida configuracao, cria imagens locais, renderiza manifests sinteticos e valida Kubernetes em modo client.
- `Estoque Deploy`: manual por `workflow_dispatch`, somente `main`, exige confirmacao `DEPLOY`, busca metadados no SSM, valida recursos AWS por metadata, publica imagens e aplica migration antes do Deployment.
- `Estoque Rollback`: manual por `workflow_dispatch`, somente `main`, exige `ROLLBACK`, valida tag no ECR e altera somente a imagem runtime. Nao executa migration reversa.

Execucao futura:

```text
GitHub -> Actions -> Estoque Deploy -> Run workflow -> main -> DEPLOY
GitHub -> Actions -> Estoque Rollback -> image_tag -> ROLLBACK
```

As validacoes reais em AWS ficam pendentes enquanto o AWS Academy estiver indisponivel.

## Build e testes locais

```powershell
dotnet build src\Oficina.Estoque.Api\Oficina.Estoque.Api.csproj
dotnet test
```

## Docker

```powershell
docker build -f Dockerfile -t oficina-estoque:local .
docker build -f Dockerfile.migration -t oficina-estoque:local-migration .
```

Este servico e consumido pelo `oficina-ordens-servico-fiap-fase4` via HTTP interno e SQS, e faz parte do ambiente Docker Compose local descrito naquele repositorio.
