param([Parameter(Mandatory=$true)][string]$GameRoot,[Parameter(Mandatory=$true)][string]$Output)
$ErrorActionPreference='Stop'
$GameRoot=[IO.Path]::GetFullPath($GameRoot).TrimEnd('\')
$Output=[IO.Path]::GetFullPath($Output).TrimEnd('\')
if($Output.StartsWith($GameRoot+'\',[StringComparison]::OrdinalIgnoreCase) -or $Output -eq $GameRoot){throw 'Use uma pasta de exportação fora do jogo.'}
if(Test-Path -LiteralPath $Output){throw 'A pasta de exportação já existe. Escolha uma nova.'}
if(-not (Test-Path -LiteralPath (Join-Path $GameRoot 'bf4.exe'))){throw 'Instalação base não encontrada.'}
$payload=Join-Path $Output 'Mod'
New-Item -ItemType Directory -Path $payload -Force | Out-Null
function Copy-GameFile([string]$relative){
    $source=Join-Path $GameRoot $relative;$target=Join-Path $payload $relative
    if(-not (Test-Path -LiteralPath $source -PathType Leaf)){throw "Arquivo necessário ausente: $relative"}
    if((Get-Item -LiteralPath $source).Attributes -band [IO.FileAttributes]::ReparsePoint){throw "Link inesperado: $relative"}
    New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force|Out-Null
    Copy-Item -LiteralPath $source -Destination $target
}
@('BF4Launcher.exe','bf4_no_drm.exe','dinput8.dll','version.json','Server\BF4BlazeServer.exe','Server\bf4_Server_Final.exe','Server\bf4_server_Retail.exe','Server\builtLevels.json','Server\dinput8_bf4hook.ini','Server\dinput8.dll','Server\Engine.BuildInfo_Win64_final_Server.dll','Server\GBClient.dll','Server\navigation.bf4pack','Server\ServerOptions.cfg','Server\xinput9_1_0.dll','Server\Instance\Admin\Startup.txt','Server\Scripts\Win32Game.cfg')|ForEach-Object {Copy-GameFile $_}
foreach($part in @('Server\assets','Server\Data\DedicatedServer')){
    $source=Join-Path $GameRoot $part
    if(-not (Test-Path -LiteralPath $source -PathType Container)){throw "Pasta necessária ausente: $part"}
    Get-ChildItem -LiteralPath $source -File -Recurse | ForEach-Object {Copy-GameFile $_.FullName.Substring($GameRoot.Length+1)}
}
$utf8=New-Object Text.UTF8Encoding($false)
$client=@'
BlazeIP=127.0.0.1
UserId=Jogador
AuthCode=
ExtraArguments=
RconHost=127.0.0.1
RconPort=25210
RconPassword=__BF4_NEW_RCON_PASSWORD__
BattlelogUrl=http://127.0.0.1/bf4/
'@
[IO.File]::WriteAllText((Join-Path $payload 'config.txt'),$client,$utf8)
$backend=[IO.File]::ReadAllText((Join-Path $GameRoot 'Server\config.txt'))
$backend=[regex]::Replace($backend,'(?im)^(ServerIP|ListenAddress|CertificatePath|CertificateKeyPath|CertificatePassword|ServerCountry)=[^\r\n]*',{param($m) switch($m.Groups[1].Value){'ServerIP'{'ServerIP=127.0.0.1'} 'ListenAddress'{'ListenAddress=127.0.0.1'} default{$m.Groups[1].Value+'='}}})
[IO.File]::WriteAllText((Join-Path $payload 'Server\config.txt'),$backend,$utf8)
$batch=[IO.File]::ReadAllText((Join-Path $GameRoot 'Server\!StartServer.bat'))
$batch=[regex]::Replace($batch,'(?im)^set ADMINPASSWORD=[^\r\n]*','set ADMINPASSWORD=__BF4_NEW_RCON_PASSWORD__')
$batch=[regex]::Replace($batch,'(?im)^set SERVERNAME=[^\r\n]*','set SERVERNAME=Offline - Operation Locker')
$batch=[regex]::Replace($batch,'(?i)\s+-PseudoPlayer\.SpawnCount\s+\d+','')
$batch=[regex]::Replace($batch,'(?im)^(start bf4_server_Final\.exe[^\r\n]*)(?=\r?$)','$1 -PseudoPlayer.SpawnCount 20')
if($batch -notmatch '__BF4_NEW_RCON_PASSWORD__'){throw 'Não foi possível sanitizar a senha do script do servidor.'}
[IO.File]::WriteAllText((Join-Path $payload 'Server\!StartServer.bat'),$batch,$utf8)
$startup=[IO.File]::ReadAllText((Join-Path $payload 'Server\Instance\Admin\Startup.txt'))
$startup=[regex]::Replace($startup,'(?im)^\s*(?:admin\.password|vars\.gamePassword)\s+[^\r\n]*','')
$startup=[regex]::Replace($startup,'(?im)^vars\.serverName\s+[^\r\n]*','vars.serverName "Offline - Operation Locker"')
[IO.File]::WriteAllText((Join-Path $payload 'Server\Instance\Admin\Startup.txt'),$startup,$utf8)
[IO.File]::WriteAllText((Join-Path $payload 'Server\Instance\Admin\MapList.txt'),"MP_Prison ConquestLarge0 1`r`n",$utf8)
[IO.File]::WriteAllText((Join-Path $payload 'user.cfg'),"GameTime.MaxVariableFps 60`r`n",$utf8)
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'install-mod.ps1'),(Join-Path $PSScriptRoot 'Instalar-Mod.cmd') -Destination $Output
$items=Get-ChildItem -LiteralPath $payload -File -Recurse | Sort-Object FullName
$hashes=@($items|ForEach-Object {(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash+'  '+$_.FullName.Substring($payload.Length+1)})
[IO.File]::WriteAllLines((Join-Path $Output 'SHA256-MOD.txt'),[string[]]$hashes,$utf8)
Write-Output ('Mod exportado: '+$items.Count+' arquivos; '+(($items|Measure-Object Length -Sum).Sum)+' bytes.')
