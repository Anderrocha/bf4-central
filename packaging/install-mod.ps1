param([string]$GameRoot,[switch]$ValidateOnly)
$ErrorActionPreference='Stop'
$source=Join-Path $PSScriptRoot 'Mod'
$manifest=Join-Path $PSScriptRoot 'SHA256-MOD.txt'
$entries=@(Get-Content -LiteralPath $manifest -Encoding UTF8 | ForEach-Object {
    if($_ -notmatch '^([A-Fa-f0-9]{64})  ([^:]+)$'){throw 'Inventário inválido.'}
    $relative=$Matches[2];$expected=$Matches[1]
    if([IO.Path]::IsPathRooted($relative) -or ($relative -split '[\\/]') -contains '..'){throw 'Caminho fora do pacote.'}
    $path=Join-Path $source $relative
    if(-not (Test-Path -LiteralPath $path -PathType Leaf)){throw "Arquivo ausente: $relative"}
    if((Get-Item -LiteralPath $path).Attributes -band [IO.FileAttributes]::ReparsePoint){throw "Link inesperado: $relative"}
    if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $expected){throw "SHA-256 divergente: $relative"}
    [PSCustomObject]@{Relative=$relative;Source=$path;Hash=$expected}
})
if($entries.Count -lt 20 -or @($entries.Relative|Sort-Object -Unique).Count -ne $entries.Count){throw 'Inventário incompleto ou duplicado.'}
Write-Host "Verificação do pacote: $($entries.Count) arquivos íntegros."
if($ValidateOnly){return}
$principal=New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if(-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){throw 'Execute Instalar-Mod.cmd como administrador.'}
if(-not $GameRoot){Add-Type -AssemblyName System.Windows.Forms;$dialog=New-Object Windows.Forms.FolderBrowserDialog;$dialog.Description='Selecione a pasta do Battlefield 4 instalado (EA ou Steam).';if($dialog.ShowDialog() -ne [Windows.Forms.DialogResult]::OK){return};$GameRoot=$dialog.SelectedPath}
$GameRoot=[IO.Path]::GetFullPath($GameRoot).TrimEnd('\')
if(-not (Test-Path -LiteralPath (Join-Path $GameRoot 'bf4.exe') -PathType Leaf)){throw 'Selecione a pasta do jogo base instalado, que contém bf4.exe.'}
if(Get-Process -Name bf4,bf4_no_drm,BF4Launcher,BF4BlazeServer,bf4_Server_Final -ErrorAction SilentlyContinue){throw 'Feche o BF4, o launcher original, o backend e o servidor antes de instalar o mod.'}
if((Get-Item -LiteralPath $GameRoot).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'A pasta do jogo não pode ser um link de diretório.'}
Write-Host "Destino: $GameRoot"
if((Read-Host 'Digite INSTALAR para copiar o mod com backup dos arquivos substituídos') -cne 'INSTALAR'){return}
$rng=[Security.Cryptography.RandomNumberGenerator]::Create();$random=New-Object byte[] 24;try{$rng.GetBytes($random)}finally{$rng.Dispose()};$password=[BitConverter]::ToString($random).Replace('-','').ToLowerInvariant()
$backup=Join-Path $env:LOCALAPPDATA ('BF4BotLauncher\mod-backups\'+(Get-Date -Format yyyyMMdd-HHmmss)+'-'+[Guid]::NewGuid().ToString('N').Substring(0,5))
$installed=New-Object 'System.Collections.Generic.List[string]'
try{
    foreach($item in $entries){
        $dest=Join-Path $GameRoot $item.Relative
        $parent=Split-Path -Parent $dest
        $check=$parent
        while($check.Length -gt $GameRoot.Length){if((Test-Path -LiteralPath $check) -and ((Get-Item -LiteralPath $check).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw "Pasta vinculada no destino: $check"};$check=Split-Path -Parent $check}
        if(Test-Path -LiteralPath $dest){if((Get-Item -LiteralPath $dest).Attributes -band [IO.FileAttributes]::ReparsePoint){throw "Link no destino: $dest"};$saved=Join-Path $backup $item.Relative;New-Item -ItemType Directory -Path (Split-Path -Parent $saved) -Force|Out-Null;Copy-Item -LiteralPath $dest -Destination $saved}
        New-Item -ItemType Directory -Path $parent -Force|Out-Null
        $tmp=$dest+'.bf4-install-'+[Guid]::NewGuid().ToString('N')+'.tmp'
        try {
            if($item.Relative -in @('config.txt','Server\!StartServer.bat')){[IO.File]::WriteAllText($tmp,[IO.File]::ReadAllText($item.Source).Replace('__BF4_NEW_RCON_PASSWORD__',$password),(New-Object Text.UTF8Encoding($false)))}else{Copy-Item -LiteralPath $item.Source -Destination $tmp}
            if(Test-Path -LiteralPath $dest){[IO.File]::Replace($tmp,$dest,$null)}else{[IO.File]::Move($tmp,$dest)}
            $installed.Add($item.Relative)
        }finally{if(Test-Path -LiteralPath $tmp){Remove-Item -LiteralPath $tmp -Force}}
    }
}catch{
    foreach($relative in $installed){$dest=Join-Path $GameRoot $relative;$saved=Join-Path $backup $relative;if(Test-Path -LiteralPath $saved){Copy-Item -LiteralPath $saved -Destination $dest -Force}else{Remove-Item -LiteralPath $dest -Force}}
    throw
}
Write-Host "Mod instalado. Backup dos arquivos substituídos: $backup"
Write-Host 'Abra a Central e selecione esta instalação. Uma senha RCON exclusiva foi criada localmente.'
