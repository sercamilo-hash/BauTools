<#
.SYNOPSIS
    Compila y despliega BauTools / ZoningFloorArea para una o varias versiones de Revit.

.DESCRIPTION
    Sustituye al antiguo ZoningFloorArea\deploy.ps1 (rutas absolutas a
    "g:\Other computers\..." y a "C:\Users\MSI\", hallazgo A5 de la auditoría).

    Despliega, por cada versión pedida:
        %APPDATA%\Autodesk\Revit\Addins\<ver>\ZoningFloorArea.addin      (manifiesto)
        %APPDATA%\Autodesk\Revit\Addins\<ver>\ZoningFloorArea\*.dll      (ensamblado)
        %APPDATA%\Autodesk\Revit\Addins\<ver>\ZoningFloorArea\Resources\ (iconos del ribbon, si existen)

    Y, si existe el bundle de pyRevit, también:
        BauTools.extension\bin\<ver>\ZoningFloorArea.dll

.PARAMETER RevitVersions
    Versiones a compilar. Por defecto 2026.  Ej: -RevitVersions 2025,2026

.PARAMETER Configuration
    Debug o Release. Por defecto Release.

.PARAMETER SkipDeploy
    Solo compila; no copia nada a las carpetas de Revit.

.EXAMPLE
    .\build\build-and-deploy.ps1
    .\build\build-and-deploy.ps1 -RevitVersions 2024,2025,2026
    .\build\build-and-deploy.ps1 -Configuration Debug -RevitVersions 2026
#>
[CmdletBinding()]
param(
    [ValidateSet(2024, 2025, 2026)]
    [int[]] $RevitVersions = @(2026),

    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [switch] $SkipDeploy
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot   = Split-Path -Parent $PSScriptRoot
$projectDir = Join-Path $repoRoot 'ZoningFloorArea'
$project    = Join-Path $projectDir 'ZoningFloorArea.csproj'
$manifest   = Join-Path $projectDir 'ZoningFloorArea.addin'

if (-not (Test-Path $project)) { throw "No se encuentra el proyecto: $project" }

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "No se encuentra 'dotnet'. Instala el .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0"
}

$failed = @()

foreach ($version in $RevitVersions) {

    $tag        = "R$($version.ToString().Substring(2))"   # 2026 -> R26
    $configName = "$Configuration $tag"

    Write-Host ""
    Write-Host "══════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host " Revit $version   ·   configuración '$configName'"     -ForegroundColor Cyan
    Write-Host "══════════════════════════════════════════════════════" -ForegroundColor Cyan

    dotnet build $project -c $configName --nologo -v minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  [X] Falló la compilación para Revit $version" -ForegroundColor Red
        $failed += $version
        continue
    }

    # AppendTargetFrameworkToOutputPath=false => bin\<Configuration>\
    $outDir = Join-Path $projectDir "bin\$configName"
    $dll    = Join-Path $outDir 'ZoningFloorArea.dll'
    if (-not (Test-Path $dll)) {
        Write-Host "  [X] Compiló pero no se encuentra el DLL en $outDir" -ForegroundColor Red
        $failed += $version
        continue
    }

    Write-Host "  [OK] Compilado: $dll" -ForegroundColor Green

    if ($SkipDeploy) { continue }

    # ── Despliegue al add-in nativo ──────────────────────────────────────
    $addinRoot   = Join-Path $env:APPDATA "Autodesk\Revit\Addins\$version"
    $addinTarget = Join-Path $addinRoot 'ZoningFloorArea'
    $resTarget   = Join-Path $addinTarget 'Resources'

    if (-not (Test-Path $addinRoot)) {
        Write-Host "  [!] $addinRoot no existe. ¿Está instalado Revit $version? Se crea igualmente." -ForegroundColor Yellow
    }
    New-Item -ItemType Directory -Force -Path $resTarget | Out-Null

    # Revit bloquea el DLL mientras está abierto: avisar en claro, no fallar en silencio.
    try {
        Copy-Item $dll (Join-Path $addinTarget 'ZoningFloorArea.dll') -Force
        Get-ChildItem $outDir -Filter '*.pdb' -ErrorAction SilentlyContinue |
            Copy-Item -Destination $addinTarget -Force -ErrorAction SilentlyContinue

        # Dependencias gestionadas que sí deben viajar (p.ej. System.Text.Json en net48).
        # RevitAPI*.dll queda excluida por ExcludeAssets="runtime" en el csproj.
        Get-ChildItem $outDir -Filter '*.dll' |
            Where-Object { $_.Name -ne 'ZoningFloorArea.dll' -and $_.Name -notlike 'RevitAPI*' } |
            Copy-Item -Destination $addinTarget -Force
    }
    catch {
        Write-Host "  [!] No se pudo copiar el DLL. Revit $version probablemente está abierto y lo tiene bloqueado. Ciérralo y repite." -ForegroundColor Yellow
        $failed += $version
        continue
    }

    Copy-Item $manifest (Join-Path $addinRoot 'ZoningFloorArea.addin') -Force

    $resSource = Join-Path $projectDir 'Resources'
    if (Test-Path $resSource) {
        Copy-Item (Join-Path $resSource '*') $resTarget -Recurse -Force
    }

    Write-Host "  [OK] Desplegado en $addinTarget" -ForegroundColor Green

    # ── Despliegue al bundle de pyRevit (opcional) ───────────────────────
    $pyRevitBin = Join-Path $repoRoot "BauTools.extension\bin\$version"
    if (Test-Path (Join-Path $repoRoot 'BauTools.extension')) {
        New-Item -ItemType Directory -Force -Path $pyRevitBin | Out-Null
        Copy-Item $dll (Join-Path $pyRevitBin 'ZoningFloorArea.dll') -Force
        Write-Host "  [OK] Desplegado en el bundle pyRevit: $pyRevitBin" -ForegroundColor Green
    }
}

Write-Host ""
if ($failed.Count -gt 0) {
    Write-Host "Terminado con errores en: $($failed -join ', ')" -ForegroundColor Red
    exit 1
}
Write-Host "Terminado correctamente." -ForegroundColor Green
