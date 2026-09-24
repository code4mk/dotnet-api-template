<#
.SYNOPSIS
    Prepares a local development environment: tools, env file, database container.
#>
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    Write-Host "Restoring .NET tools..." -ForegroundColor Cyan
    dotnet tool restore

    if (-not (Test-Path "docker/.env")) {
        Copy-Item "docker/.env.example" "docker/.env"
        Write-Host "Created docker/.env from .env.example" -ForegroundColor Yellow
    }

    Write-Host "Starting PostgreSQL..." -ForegroundColor Cyan
    docker compose -f docker/docker-compose.yml -f docker/docker-compose.override.yml up -d db

    Write-Host "Restoring and building..." -ForegroundColor Cyan
    dotnet build

    Write-Host "Done. Run the API with: dotnet run --project src/DotnetApiTemplate.Api" -ForegroundColor Green
}
finally {
    Pop-Location
}
