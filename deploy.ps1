# deploy.ps1 — Compila (Release) e instala o NavisworksToolkit no Navisworks.
# O Navisworks só carrega plugins .NET em subpasta de Plugins\ com o mesmo nome da DLL:
#   <Navisworks>\Plugins\NavisworksToolkit\NavisworksToolkit.dll
# Requer terminal como Administrador (grava em Program Files) e Navisworks FECHADO.
# Uso: ./deploy.ps1 [-NavisVersion 2026] [-Sku Simulate|Manage] [-IncludePdb]
param(
    [string]$NavisVersion = '2026',
    [ValidateSet('Simulate', 'Manage')]
    [string]$Sku = 'Simulate',
    [switch]$IncludePdb
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$navisDir = "C:\Program Files\Autodesk\Navisworks $Sku $NavisVersion"
if (-not (Test-Path $navisDir)) { throw "Navisworks não encontrado em: $navisDir" }

& "$PSScriptRoot\build.ps1" -Configuration Release

$src = Join-Path $PSScriptRoot 'src\bin\Release'
$dst = Join-Path $navisDir 'Plugins\NavisworksToolkit'

Write-Host "`nInstalando em $dst..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force $dst | Out-Null

Copy-Item (Join-Path $src 'NavisworksToolkit.dll') $dst -Force
Copy-Item (Join-Path $src '*.png') $dst -Force
New-Item -ItemType Directory -Force (Join-Path $dst 'en-US') | Out-Null
Copy-Item (Join-Path $src 'en-US\NavisworksToolkit.xaml') (Join-Path $dst 'en-US') -Force
if ($IncludePdb) { Copy-Item (Join-Path $src 'NavisworksToolkit.pdb') $dst -Force }

Write-Host "Instalado. Reinicie o Navisworks — a tab 'Navisworks Toolkit' aparecerá no ribbon." -ForegroundColor Green
