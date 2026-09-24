<#
.SYNOPSIS
    Applies pending EF Core migrations to the database in the current connection string.
#>
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    dotnet tool restore
    dotnet ef database update --project src/DotnetApiTemplate.Api
}
finally {
    Pop-Location
}
