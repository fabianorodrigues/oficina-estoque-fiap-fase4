# Kubernetes

Manifests para publicacao futura do microservico Estoque no cluster `oficina`, namespace `oficina`.

## Arquitetura

SQS comandos -> Consumer -> Inbox -> Regra de estoque -> Outbox -> Dispatcher -> SQS eventos.

## Decisoes

- Deployment `oficina-estoque` com 1 replica.
- Strategy `Recreate`.
- Service `ClusterIP`.
- Sem HPA.
- Consumer SQS com concorrencia 1 e receive de 1 mensagem.
- Runtime monta somente a connection string via Secret Store CSI em `/mnt/secrets-store`.
- Migration usa SecretProviderClass proprio e executa `/app/efbundle` antes do Deployment.

## Secrets

- Runtime: `/oficina/estoque/runtime-db`.
- Migration: `/oficina/estoque/migration-db`.

As senhas e connection strings sao geridas fora deste repositorio e nao devem ser versionadas.

## Renderizacao

Use `scripts/render-k8s-manifests.ps1` com imagens e URLs obtidas pela pipeline. Os templates nao contem URLs reais de SQS, ECR real, account id ou secrets.
