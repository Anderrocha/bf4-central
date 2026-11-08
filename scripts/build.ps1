[CmdletBinding()]
param([switch]$TestBuild)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'Compilador .NET Framework 4.x não encontrado.' }
$outputFolder = Join-Path $projectRoot 'dist'
New-Item -ItemType Directory -Path $outputFolder -Force | Out-Null
$manifestName = if ($TestBuild) { 'app.manifest' } else { 'admin.manifest' }
$outputName = if ($TestBuild) { 'BF4 Central.Tests.exe' } else { 'BF4 Central.exe' }
$outputPath = Join-Path $outputFolder $outputName
$sourceFiles = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src') -Filter '*.cs' -File | Sort-Object Name | ForEach-Object { $_.FullName })
$compilerArgs = @('/nologo', '/target:winexe', '/platform:x64', '/optimize+', '/main:BF4Bots.UnifiedProgram', "/out:$outputPath", "/win32manifest:$(Join-Path $projectRoot $manifestName)", '/reference:System.Windows.Forms.dll', '/reference:System.Drawing.dll', '/reference:System.Core.dll', '/reference:System.Management.dll', '/reference:System.Web.Extensions.dll', "/resource:$(Join-Path $projectRoot 'resources\maps.json'),BF4Bots.Catalog")
$compilerArgs += "/win32icon:$(Join-Path $projectRoot 'resources\central.ico')"
$art=Join-Path $projectRoot 'local-assets\maps.resources'
if (Test-Path -LiteralPath $art) { $compilerArgs += "/resource:$art,BF4Bots.MapArt.resources" }
& $compiler @compilerArgs @sourceFiles
if ($LASTEXITCODE -ne 0) { throw "Compilação falhou: $LASTEXITCODE" }
Write-Output $outputPath
