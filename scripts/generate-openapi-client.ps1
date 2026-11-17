<#
.SYNOPSIS
    Regenerates the strongly-typed C# OpenAPI client for VividImagination.Mobile using NSwag.
.DESCRIPTION
    Reads the OpenAPI specification from OpenApi/vividimagination-v1.json and executes NSwag
    to generate VividImaginationClient.cs.
#>

[CmdletBinding()]
param(
    [string]$SpecPath = "$PSScriptRoot/../src/mobile/VividImagination.Mobile/OpenApi/nswag.json"
)

$ErrorActionPreference = "Stop"

Write-Host "Verifying NSwag CLI..." -ForegroundColor Cyan
if (-not (Get-Command "nswag" -ErrorAction SilentlyContinue)) {
    Write-Warning "NSwag CLI not found globally. Installing nswag.consolecore..."
    dotnet tool install -g nswag.consolecore
}

Write-Host "Generating OpenAPI client from $SpecPath..." -ForegroundColor Cyan
nswag run $SpecPath

if ($LASTEXITCODE -eq 0) {
    Write-Host "Successfully generated OpenAPI Client!" -ForegroundColor Green
} else {
    Write-Error "NSwag execution failed with exit code $LASTEXITCODE."
}
