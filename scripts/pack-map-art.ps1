param()
$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
$source=Join-Path $projectRoot 'local-assets\map-images'
if (-not (Test-Path -LiteralPath $source)) { throw 'Coloque as imagens locais em local-assets\map-images. Nenhuma imagem e baixada por este script.' }
$target=Join-Path $projectRoot 'local-assets\maps.resources'
$writer=New-Object System.Resources.ResourceWriter($target)
try {
    Get-ChildItem -LiteralPath $source -Filter '*.jpg' -File | ForEach-Object {
        if ($_.Length -gt 2000000) { throw "Imagem excessiva: $($_.Name)" }
        $writer.AddResource($_.BaseName, [object][IO.File]::ReadAllBytes($_.FullName))
    }
    $banner=Join-Path $projectRoot 'local-assets\banner.jpg'
    if (Test-Path -LiteralPath $banner) { $writer.AddResource('__banner', [object][IO.File]::ReadAllBytes($banner)) }
    $writer.Generate()
} finally { $writer.Close() }
Write-Output $target
