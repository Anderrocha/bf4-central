[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
& (Join-Path $PSScriptRoot 'build.ps1') -TestBuild
$fixtureRoot = Join-Path ([IO.Path]::GetTempPath()) ('bf4-central-fixture-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixtureRoot -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $projectRoot 'tests\fixtures\config.example.txt') -Destination (Join-Path $fixtureRoot 'config.txt')
$resultFolder = Join-Path $projectRoot 'tests\results'
New-Item -ItemType Directory -Path $resultFolder -Force | Out-Null
$report = Join-Path $resultFolder 'unified-results.txt'
$previousRoot = $env:BF4_CENTRAL_TEST_ROOT
try {
    $env:BF4_CENTRAL_TEST_ROOT = $fixtureRoot
    $exe = Join-Path $projectRoot 'dist\BF4 Central.Tests.exe'
    $testProcess = Start-Process -FilePath $exe -ArgumentList @('--unified-test', ('"' + $report + '"')) -WindowStyle Hidden -PassThru -Wait
    if (Test-Path -LiteralPath $report) { Get-Content -LiteralPath $report }
    if ($testProcess.ExitCode -ne 0) { throw "Testes falharam: $($testProcess.ExitCode)" }
} finally {
    $env:BF4_CENTRAL_TEST_ROOT = $previousRoot
}
# Keep the uniquely named temporary fixture for diagnosis; no game installation is changed.
