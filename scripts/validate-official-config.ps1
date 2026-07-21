param(
    [string]$ConfigPath = "config/official.json"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

$raw = Get-Content -LiteralPath $ConfigPath -Raw
$config = $raw | ConvertFrom-Json

Assert-True ($config.version -eq 1) "Versao de official.json invalida."
Assert-True ($config.application.name -eq "oficina-estoque") "Aplicacao oficial invalida."
Assert-True ($config.application.environment -eq "Production") "Ambiente oficial deve ser Production."
Assert-True ($config.application.containerPort -eq 8080) "Porta oficial invalida."
Assert-True ($config.ecs.serviceName -eq "oficina-estoque") "ECS service oficial invalido."
Assert-True ($config.ecs.containerName -eq "oficina-estoque") "ECS container oficial invalido."
Assert-True ($config.ecs.migrationContainerName -eq "oficina-estoque-migration") "ECS migration container oficial invalido."
Assert-True ($config.ecs.desiredCount -eq 1) "Desired count deve ser 1."
Assert-True ($config.ecs.launchType -eq "FARGATE") "Launch type deve ser FARGATE."
Assert-True ($config.queues.consumerConcurrency -eq 1) "Consumer concurrency deve ser 1."
Assert-True ($config.queues.maxMessagesPerReceive -eq 1) "Max messages por receive deve ser 1."
Assert-True ($config.queues.waitTimeSeconds -eq 20) "Wait time deve ser 20."
Assert-True ($config.queues.visibilityTimeoutSeconds -eq 60) "Visibility timeout deve ser 60."
Assert-True ($config.health.path -eq "/health") "Health path invalido."
Assert-True ($config.health.readinessPath -eq "/ready") "Readiness path invalido."
Assert-True ($config.secrets.runtimeDatabase -ne $config.secrets.migrationDatabase) "Secrets runtime e migration devem ser distintos."

$paths = @(
    $config.aws.clusterNameParameter,
    $config.aws.ecrRepositoryParameter,
    $config.ecs.targetGroupArnParameter,
    $config.ecs.logGroupNameParameter,
    $config.ecs.taskSecurityGroupParameter,
    $config.ecs.privateSubnet1Parameter,
    $config.ecs.privateSubnet2Parameter,
    $config.secrets.runtimeDatabase,
    $config.secrets.migrationDatabase,
    $config.queues.commandsUrlParameter,
    $config.queues.commandsArnParameter,
    $config.queues.commandsDlqUrlParameter,
    $config.queues.commandsDlqArnParameter,
    $config.queues.eventsUrlParameter,
    $config.queues.eventsArnParameter,
    $config.queues.eventsDlqUrlParameter,
    $config.queues.eventsDlqArnParameter
)
foreach ($path in $paths) {
    Assert-True ($path.StartsWith("/oficina/")) "Parametro fora do prefixo /oficina/: $path"
}

$forbiddenPatterns = @(
    "Password\s*=",
    "ConnectionStrings?\s*[=:]",
    "SecretString",
    "AWS_ACCESS_KEY_ID\s*=",
    "AWS_SECRET_ACCESS_KEY\s*=",
    "AWS_SESSION_TOKEN\s*=",
    "https://sqs\.",
    "\.amazonaws\.com/[0-9]{12}/",
    "[0-9]{12}\.dkr\.ecr\.",
    "\b[0-9]{12}\b",
    "Fase3",
    "fase-3",
    "/dev/",
    "-dev",
    "-hml",
    "-prod"
)
foreach ($pattern in $forbiddenPatterns) {
    Assert-True (-not [regex]::IsMatch($raw, $pattern, [Text.RegularExpressions.RegexOptions]::IgnoreCase)) "Conteudo proibido encontrado: $pattern"
}

Write-Host "official.json valido."
