param(
    [Parameter(Mandatory = $true)][string]$BaseUrl,
    [string]$CommandsQueueUrl = "",
    [string]$AwsRegion = "us-east-1",
    [string]$LocalStackEndpoint = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$health = Invoke-RestMethod -Uri "$BaseUrl/health" -Method Get -TimeoutSec 10
if ($health.status -ne "Healthy") { throw "Health check falhou." }

$ready = Invoke-RestMethod -Uri "$BaseUrl/ready" -Method Get -TimeoutSec 10
if ($ready.status -ne "Ready") { throw "Readiness check falhou." }

if ($CommandsQueueUrl -and $LocalStackEndpoint) {
    Write-Host "Smoke assincrono LocalStack habilitado apenas para ambiente local sintetico."
    aws --endpoint-url $LocalStackEndpoint --region $AwsRegion sqs get-queue-attributes --queue-url $CommandsQueueUrl --attribute-names ApproximateNumberOfMessages | Out-Null
}

Write-Host "Smoke test concluido."
