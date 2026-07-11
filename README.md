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

## Build e testes locais

```powershell
dotnet build src\Oficina.Estoque.Api\Oficina.Estoque.Api.csproj
dotnet test
```

## Docker

```powershell
docker build -f docker/Dockerfile -t oficina-estoque-api .
```

Este servico e consumido pelo `oficina-ordens-servico-fiap-fase4` via HTTP interno e SQS, e faz parte do ambiente Docker Compose local descrito naquele repositorio.
