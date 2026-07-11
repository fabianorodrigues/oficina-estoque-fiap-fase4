param(
    [Parameter(Mandatory = $true)][string]$Namespace,
    [Parameter(Mandatory = $true)][string]$ExpectedImageTag,
    [string]$DeploymentName = "oficina-estoque",
    [string]$ServiceName = "oficina-estoque"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Write-Host "Validador read-only do Estoque."
Write-Host "Comandos permitidos para uso futuro: aws sts get-caller-identity, aws ssm get-parameter, aws ecr describe-images, aws sqs get-queue-attributes, aws secretsmanager describe-secret, kubectl get, kubectl describe, kubectl logs, kubectl rollout status."
Write-Host "Comandos proibidos: kubectl apply, kubectl delete, aws put-*, aws create-*, aws update-*, aws delete-*, aws secretsmanager get-secret-value."

kubectl get deployment $DeploymentName -n $Namespace -o json | Out-Null
kubectl get service $ServiceName -n $Namespace -o json | Out-Null
kubectl rollout status deployment/$DeploymentName -n $Namespace --timeout=120s

$deployment = kubectl get deployment $DeploymentName -n $Namespace -o json | ConvertFrom-Json
if ($deployment.spec.strategy.type -ne "Recreate") { throw "Deployment nao usa Recreate." }
if ($deployment.spec.replicas -ne 1) { throw "Deployment nao possui 1 replica desejada." }
if ($deployment.status.readyReplicas -ne 1) { throw "Deployment nao possui 1 replica Ready." }
$image = $deployment.spec.template.spec.containers[0].image
if ($image -notmatch [regex]::Escape(":$ExpectedImageTag")) { throw "Imagem nao contem a tag esperada." }

$hpa = kubectl get hpa -n $Namespace --ignore-not-found
if ($hpa -match $DeploymentName) { throw "HPA encontrado para oficina-estoque." }

Write-Host "Validacao read-only concluida."
