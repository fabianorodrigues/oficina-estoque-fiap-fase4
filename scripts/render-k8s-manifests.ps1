param(
    [string]$ConfigPath = "config/official.json",
    [Parameter(Mandatory = $true)][string]$OutputDirectory,
    [Parameter(Mandatory = $true)][string]$RuntimeImage,
    [Parameter(Mandatory = $true)][string]$MigrationImage,
    [Parameter(Mandatory = $true)][string]$AwsRegion,
    [Parameter(Mandatory = $true)][string]$CommandsQueueUrl,
    [Parameter(Mandatory = $true)][string]$CommandsDlqUrl,
    [Parameter(Mandatory = $true)][string]$EventsQueueUrl,
    [Parameter(Mandatory = $true)][string]$EventsDlqUrl,
    [ValidateSet("PodIdentity", "IRSA")][string]$WorkloadIdentityMode = "PodIdentity",
    [string]$RuntimeIrsaRoleArn = "",
    [string]$MigrationIrsaRoleArn = "",
    [Parameter(Mandatory = $true)][string]$MigrationJobName,
    [string]$OtelExporterOtlpEndpoint = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

& "$PSScriptRoot/validate-official-config.ps1" -ConfigPath $ConfigPath

function Assert-Url([string]$Value, [string]$Name) {
    if (-not [Uri]::TryCreate($Value, [UriKind]::Absolute, [ref]([Uri]$null))) {
        throw "$Name invalida."
    }
}
function Assert-Image([string]$Value, [string]$Name) {
    if ($Value -match ":(latest|dev|hml|staging|prod)$") { throw "$Name usa tag proibida." }
    if ($Value -notmatch ":[A-Za-z0-9._-]+$") { throw "$Name deve conter tag explicita." }
}

Assert-Image $RuntimeImage "RuntimeImage"
Assert-Image $MigrationImage "MigrationImage"
Assert-Url $CommandsQueueUrl "CommandsQueueUrl"
Assert-Url $CommandsDlqUrl "CommandsDlqUrl"
Assert-Url $EventsQueueUrl "EventsQueueUrl"
Assert-Url $EventsDlqUrl "EventsDlqUrl"
if ($WorkloadIdentityMode -eq "PodIdentity" -and ($RuntimeIrsaRoleArn -or $MigrationIrsaRoleArn)) {
    throw "Nao habilite Pod Identity e IRSA simultaneamente."
}
if ($WorkloadIdentityMode -eq "IRSA" -and (-not $RuntimeIrsaRoleArn -or -not $MigrationIrsaRoleArn)) {
    throw "IRSA requer roles runtime e migration."
}
if ($MigrationJobName -notmatch "^oficina-estoque-migration-[a-zA-Z0-9-]+$") {
    throw "Nome do Migration Job invalido."
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$templateDir = Join-Path (Split-Path $PSScriptRoot -Parent) "deploy/k8s"
$replacements = @{
    "{{RUNTIME_IMAGE}}" = $RuntimeImage
    "{{MIGRATION_IMAGE}}" = $MigrationImage
    "{{AWS_REGION}}" = $AwsRegion
    "{{COMMANDS_QUEUE_URL}}" = $CommandsQueueUrl
    "{{COMMANDS_DLQ_URL}}" = $CommandsDlqUrl
    "{{EVENTS_QUEUE_URL}}" = $EventsQueueUrl
    "{{EVENTS_DLQ_URL}}" = $EventsDlqUrl
    "{{MIGRATION_JOB_NAME}}" = $MigrationJobName
    "{{OTEL_EXPORTER_OTLP_ENDPOINT}}" = $OtelExporterOtlpEndpoint
    "{{RUNTIME_IRSA_ANNOTATIONS}}" = ""
    "{{MIGRATION_IRSA_ANNOTATIONS}}" = ""
}
if ($WorkloadIdentityMode -eq "IRSA") {
    $replacements["{{RUNTIME_IRSA_ANNOTATIONS}}"] = "  annotations:`n    eks.amazonaws.com/role-arn: `"$RuntimeIrsaRoleArn`""
    $replacements["{{MIGRATION_IRSA_ANNOTATIONS}}"] = "  annotations:`n    eks.amazonaws.com/role-arn: `"$MigrationIrsaRoleArn`""
}

$files = @(
    "service-account-runtime.template.yaml",
    "service-account-migration.template.yaml",
    "secret-provider-class-runtime.template.yaml",
    "secret-provider-class-migration.template.yaml",
    "configmap.template.yaml",
    "service.yaml",
    "migration-job.template.yaml",
    "deployment.template.yaml"
)
foreach ($file in $files) {
    $content = Get-Content -LiteralPath (Join-Path $templateDir $file) -Raw
    foreach ($key in $replacements.Keys) {
        $content = $content.Replace($key, $replacements[$key])
    }
    if ($content -match "{{[^}]+}}") { throw "Placeholder pendente em $file." }
    $targetName = $file.Replace(".template", "")
    $targetPath = Join-Path $OutputDirectory $targetName
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($targetPath, $content, $utf8NoBom)
}

Write-Host "Manifests renderizados em $OutputDirectory."
