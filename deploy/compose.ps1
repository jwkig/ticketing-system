#requires -Version 5.1
<#
.SYNOPSIS
  Convenience wrapper around `docker compose` for the ticketing system's
  dev / test / prod environments.

.DESCRIPTION
  Composes the base docker-compose.yml with the chosen environment override,
  a per-environment project name (so environments don't collide), and the
  matching env/<env>.env file.

.EXAMPLE
  ./deploy/compose.ps1 dev up
.EXAMPLE
  ./deploy/compose.ps1 prod up -Detach
.EXAMPLE
  ./deploy/compose.ps1 test down
#>
param(
    [Parameter(Mandatory)][ValidateSet('dev', 'test', 'prod')][string]$Environment,
    [Parameter(Mandatory)][ValidateSet('up', 'down', 'logs', 'build', 'ps')][string]$Action,
    [switch]$Detach
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    $envFile = Join-Path 'env' "$Environment.env"
    if (-not (Test-Path $envFile)) {
        throw "Missing $envFile. Copy env/$Environment.env.example to it and fill in values."
    }

    $composeArgs = @(
        'compose',
        '-f', 'docker-compose.yml',
        '-f', "docker-compose.$Environment.yml",
        '-p', "ticketing-$Environment",
        '--env-file', $envFile,
        $Action
    )

    switch ($Action) {
        'up' { $composeArgs += '--build'; if ($Detach) { $composeArgs += '-d' } }
        'logs' { $composeArgs += '-f' }
    }

    & docker @composeArgs
}
finally {
    Pop-Location
}
