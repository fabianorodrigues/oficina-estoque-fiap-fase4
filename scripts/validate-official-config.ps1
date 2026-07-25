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
Assert-True ($null -eq $config.PSObject.Properties['ecs']) "Bloco ecs removido: use kubernetes."
Assert-True ($config.kubernetes.deploymentName -eq "oficina-estoque") "Deployment oficial invalido."
Assert-True ($config.kubernetes.serviceName -eq "oficina-estoque") "Service oficial invalido."
Assert-True ($config.kubernetes.containerName -eq "oficina-estoque") "Container oficial invalido."
Assert-True ($config.kubernetes.migrationJobPrefix -eq "oficina-estoque-migration") "Prefixo do Migration Job invalido."
Assert-True ($config.kubernetes.replicas -eq 1) "Replicas deve ser 1."
Assert-True ($config.kubernetes.nodePort -ge 30000 -and $config.kubernetes.nodePort -le 32767) "NodePort fora da faixa 30000-32767."
foreach ($manifestKey in @('configMap', 'deployment', 'service', 'migrationJob', 'secretApp', 'secretMigration')) {
    $manifestPath = $config.kubernetes.manifests.$manifestKey
    Assert-True ((-not [string]::IsNullOrWhiteSpace($manifestPath)) -and (Test-Path -LiteralPath $manifestPath -PathType Leaf)) "Manifesto ausente: $manifestKey"
}
# Um Secret unico servindo Deployment e Job daria ao runtime a credencial de
# migration; os dois templates precisam ser arquivos distintos.
Assert-True ($config.kubernetes.manifests.secretApp -ne $config.kubernetes.manifests.secretMigration) "secretApp e secretMigration devem ser manifests distintos."
Assert-True ($config.deploy.s3Prefix -eq "k8s-deploy/estoque/") "deploy.s3Prefix invalido."
Assert-True ($config.deploy.presignedUrlTtlSeconds -gt 0 -and $config.deploy.presignedUrlTtlSeconds -le 300) "TTL da URL pre-assinada deve ficar entre 1 e 300 segundos."
Assert-True ($config.coverage.minimumLinePercentage -ge 80) "Cobertura minima deve ser ao menos 80."
Assert-True ($config.queues.consumerConcurrency -eq 1) "Consumer concurrency deve ser 1."
Assert-True ($config.queues.maxMessagesPerReceive -eq 1) "Max messages por receive deve ser 1."
Assert-True ($config.queues.waitTimeSeconds -eq 20) "Wait time deve ser 20."
Assert-True ($config.queues.visibilityTimeoutSeconds -eq 60) "Visibility timeout deve ser 60."
Assert-True ($config.health.path -eq "/health") "Health path invalido."
Assert-True ($config.health.readinessPath -eq "/ready") "Readiness path invalido."
Assert-True ($config.secrets.runtimeDatabase -ne $config.secrets.migrationDatabase) "Secrets runtime e migration devem ser distintos."

$paths = @(
    $config.aws.namespaceParameter,
    $config.aws.instanceIdParameter,
    $config.aws.ecrRepositoryParameter,
    $config.secrets.runtimeDatabase,
    $config.secrets.migrationDatabase,
    $config.kubernetes.targetGroupArnParameter,
    $config.kubernetes.nodePortParameter,
    $config.deploy.parameterPathPrefix,
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
    "/dev/",
    "-dev",
    "-hml",
    "-prod"
)
foreach ($pattern in $forbiddenPatterns) {
    Assert-True (-not [regex]::IsMatch($raw, $pattern, [Text.RegularExpressions.RegexOptions]::IgnoreCase)) "Conteudo proibido encontrado: $pattern"
}

Write-Host "official.json valido."
