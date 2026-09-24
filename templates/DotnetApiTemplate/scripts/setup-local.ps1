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

    if (-not (Test-Path ".env")) {
        Copy-Item ".env.example" ".env"
        Write-Host "Created .env from .env.example" -ForegroundColor Yellow
    }

    Write-Host "Starting PostgreSQL..." -ForegroundColor Cyan
    docker compose --env-file .env -f docker/docker-compose.yml -f docker/docker-compose.override.yml up -d db

    Write-Host "Restoring and building..." -ForegroundColor Cyan
    dotnet build

    Write-Host "Done. Run the API with auto reload: dotnet watch --project src/DotnetApiTemplate.Api" -ForegroundColor Green
}
finally {
    Pop-Location
}
