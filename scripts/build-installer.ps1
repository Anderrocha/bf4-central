$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$dist=Join-Path $root 'dist'
$exe=Join-Path $dist 'BF4 Central.exe'
if(-not (Test-Path -LiteralPath $exe)){throw 'Compile a Central com build.cmd antes de gerar o instalador.'}
$staging=Join-Path ([IO.Path]::GetTempPath()) ('BF4Central-payload-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $staging | Out-Null
Copy-Item -LiteralPath $exe -Destination $staging
$readme=Join-Path $staging 'LEIA-ME.txt'
[IO.File]::WriteAllText($readme,"BF4 Central para Windows x64`r`nAbra BF4 Central no menu Iniciar. A Central localiza a instalacao do jogo e do cliente offline.`r`nO instalador nao instala o jogo base nem o mod. Para instalar o mod use a pasta 02-Mod-privado.`r`nPara remover a Central, feche o programa, remova o atalho e a pasta local Programs\BF4 Central. Os arquivos do jogo ficam separados.`r`n",[Text.Encoding]::UTF8)
$lines=@('BF4 Central.exe','LEIA-ME.txt') | ForEach-Object {(Get-FileHash -LiteralPath (Join-Path $staging $_) -Algorithm SHA256).Hash+'  '+$_}
[IO.File]::WriteAllLines((Join-Path $staging 'SHA256.txt'),[string[]]$lines,(New-Object Text.UTF8Encoding($false)))
$payload=Join-Path $staging 'payload.zip'
Compress-Archive -LiteralPath @((Join-Path $staging 'BF4 Central.exe'),$readme,(Join-Path $staging 'SHA256.txt')) -DestinationPath $payload
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$output=Join-Path $dist 'BF4 Central Setup.exe'
& $compiler '/nologo' '/target:winexe' '/platform:x64' '/optimize+' "/out:$output" "/win32manifest:$(Join-Path $root 'app.manifest')" "/win32icon:$(Join-Path $root 'resources\central.ico')" '/reference:System.Windows.Forms.dll' '/reference:System.Drawing.dll' '/reference:System.Core.dll' '/reference:System.IO.Compression.dll' '/reference:Microsoft.CSharp.dll' "/resource:$payload,BF4Central.Payload" (Join-Path $root 'packaging\Setup.cs')
if($LASTEXITCODE -ne 0){throw 'Falha ao compilar instalador.'}
Write-Output $output
