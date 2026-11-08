# Distribuição e reinstalação

Na Central 0.5, a etapa **Instalação** pode verificar a pasta do jogo e instalar o mod diretamente de `02-Mod-privado`. O instalador integrado é o script embutido no EXE; ele não executa scripts da pasta selecionada. Após a instalação, avance por Jogo, Bots, Amigos (opcional) e Servidor. O indicador de 100% confirma arquivos e configuração para iniciar; conexão e entrada na partida são verificadas no início real. O jogo base continua sendo instalado pela biblioteca EA ou Steam.

O pacote de uso pessoal tem três partes: **01-Central** (instalador e EXE portátil), **02-Mod-privado** (componentes do mod para aplicar ao jogo instalado) e **03-Projeto** (fontes, imagens locais, documentação, scripts e testes). O Battlefield 4 base e DLCs são instalados pela EA app ou Steam; não fazem parte deste pacote.

## Instalar em outro PC

1. Instale o Battlefield 4 e as expansões pela sua biblioteca EA ou Steam.
2. Extraia a pasta completa. Se o mod compatível ainda não estiver instalado, abra **02-Mod-privado/Instalar-Mod.cmd** como administrador, escolha a pasta que contém `bf4.exe` e confirme `INSTALAR`. Feche o jogo, o launcher original e os serviços locais antes disso.
3. Abra **01-Central/BF4 Central Setup.exe**. Ele instala em `%LOCALAPPDATA%\Programs\BF4 Central` e cria o atalho no menu Iniciar. O EXE portátil pode ser aberto diretamente se preferir.
4. Abra a Central, escolha a instalação EA/Steam se necessário e use **Iniciar tudo e jogar**. Para jogar na LAN, prepare a rede e gere novos convites; os anteriores não fazem parte do pacote.

O instalador da Central verifica SHA-256 e faz backup local de arquivos substituídos. O instalador do mod verifica o inventário completo, gera uma senha RCON aleatória por instalação e salva backup dos arquivos substituídos em `%LOCALAPPDATA%\BF4BotLauncher\mod-backups`. A configuração exportada começa em loopback; o recurso de LAN continua sendo uma ação explícita no launcher. O pacote não carrega contas, logs de partidas, convites, certificados privados nem as senhas anteriores. A instalação limpa do mod com o jogo real ainda precisa ser testada no computador de destino.

## Gerar a distribuição

- Execute `build.cmd`, depois `scripts/build-installer.ps1`. O instalador pode ser validado com `--self-test <relatório>`, que instala e atualiza apenas em uma pasta temporária.
- Para uso pessoal com sua instalação existente, `packaging/export-mod.ps1 -GameRoot <pasta do BF4> -Output <nova pasta>` cria um conjunto selecionado de arquivos, configurações sanitizadas e `SHA256-MOD.txt`. `install-mod.ps1 -ValidateOnly` faz verificação sem instalação. Nunca inclua o pacote do mod ou dados pessoais no Git.
- O GitHub guarda código, instalador reproduzível, testes e documentação. O workflow compila e disponibiliza o instalador sem os arquivos de terceiros. O banner e as imagens embutidas no EXE local ficam em `local-assets/` e não são enviados ao repositório.

Para remover a Central instalada, feche o programa e remova o atalho e a pasta `%LOCALAPPDATA%\Programs\BF4 Central`. A Central não desinstala o Battlefield 4 nem o mod.
