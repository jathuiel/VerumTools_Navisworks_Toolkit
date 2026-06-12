# build.ps1 — Compila a solução consolidada NavisworksToolkit.
# Uso: ./build.ps1 [-Configuration Release|Debug]
param(
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

Write-Host "Compilando NavisworksToolkit ($Configuration)..." -ForegroundColor Cyan
dotnet build NavisworksToolkit.sln -c $Configuration --nologo
if ($LASTEXITCODE -ne 0) { throw "Falha na compilação (exit $LASTEXITCODE)." }

$out = Join-Path $PSScriptRoot "src\bin\$Configuration"
Write-Host "`nSaída: $out" -ForegroundColor Green
Get-ChildItem $out -File | Format-Table Name, Length -AutoSize
