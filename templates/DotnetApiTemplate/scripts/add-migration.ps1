<#
.SYNOPSIS
    Adds an EF Core migration to the API project.
.EXAMPLE
    ./scripts/add-migration.ps1 -Name InitialCreate
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$Name
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    dotnet tool restore
    dotnet ef migrations add $Name `
        --project src/DotnetApiTemplate.Api `
        --output-dir Data/Migrations
}
finally {
    Pop-Location
}
