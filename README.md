# BF4 Central

Central local para Windows que reúne início do jogo, perfis de bots e administração do servidor de Battlefield 4. O ponto de entrada é um EXE próprio; os componentes do jogo e do mod continuam na instalação local.

## Funcionalidades

- Abas Jogo, Bots e desempenho e Servidor.
- Início coordenado de backend, servidor e cliente, reaproveitando processos existentes.
- Identificação do conflito da porta 3216 com o EA App. Somente processos conhecidos e com caminho conferido podem ser encerrados.
- Início do cliente com -blazeIP 127.0.0.1 -patchSSL e verificação da conexão TCP do processo do jogo ao backend local.
- Cinco perfis comportamentais: Noob, Fácil, Normal, Difícil e Extremo. Precisão e cadência das balas permanecem no padrão do mod.
- Modo leve, redução de logs e rotinas cosméticas e limite de quadros no arquivo local do jogo.
- Cliente RCON com autenticação por desafio/hash, catálogo de mapas e edição de rotação.
- Gravação local atômica da rotação com backup e recarga, como alternativa ao mapList.add que falhou na versão examinada.
- Seleção única entre as listas, alvo explícito e botão dedicado a Operation Locker.
- Jogo imediato com um único mapa na rotação, evitando requisitos de expansão vindos de mapas não escolhidos.

## Compilar

Requer Windows x64 e compilador do .NET Framework 4.x. Não usa pacotes NuGet.

    .\build.cmd

Saída: dist/BF4 Central.exe. O EXE final solicita elevação para gerenciar a instalação em Program Files. Se scripts estiverem bloqueados por uma política administrada, use o procedimento permitido pela sua administração; o script de compilação mostra a chamada ao compilador.

## Testes isolados

    .\test.cmd

Os testes usam configuração fictícia, servidor RCON simulado e uma raiz temporária; não precisam do jogo nem alteram a instalação real. Os resultados e capturas de renderização são gerados em tests/results. O workflow do GitHub compila no Windows e disponibiliza o EXE como artefato.

## Uso com a instalação local

A instalação padrão esperada é C:\Program Files\EA Games\Battlefield 4. Outra pasta pode ser passada com --game "D:\Games\Battlefield 4". São necessários o jogo, suas expansões desejadas e os auxiliares compatíveis já instalados. Este repositório não fornece executáveis, DLLs ou arquivos de jogo de terceiros.

- --play: inicia os componentes e o cliente.
- --server: abre a aba Servidor e conecta por RCON.
- --map MP_Prison: prepara Operation Locker e abre o cliente.
- --locker-repair: isola Locker, atualiza o nome e reinicia o servidor para renovar seu anúncio. Essa operação interrompe uma partida local existente.

Na aba Servidor, Operation Locker é o atalho prioritário. Jogar escolhido usa o alvo indicado na tela. Só rotação edita a lista avançada sem trocar o mapa imediatamente. Quando o jogador está fora de uma partida, ainda deve entrar no servidor pelo menu do BF4; a Central não automatiza essa entrada.

## Estado da validação

Compilação e testes de protocolo, rotação, recuperação e seleção de mapas foram executados localmente. A conexão real ao backend offline e a leitura RCON também foram verificadas. Houve confirmação de Operation Locker ativo e, posteriormente, de rotação exclusiva com reinício do servidor. A ausência do aviso de DLC ainda requer validação dentro da partida. Consulte [validação](docs/VALIDATION.md) e [limitações](docs/KNOWN_ISSUES.md).

## Arquivos locais e backups

As credenciais são lidas da configuração da instalação. Não coloque config.txt, perfis PROFSAVE, logs pessoais nem backups no Git. O aplicativo salva backups e relatórios em %LOCALAPPDATA%\BF4BotLauncher.

A [evolução](CHANGELOG.md), a [arquitetura](docs/ARCHITECTURE.md) e o código em src registram o avanço do projeto. O catálogo contém somente identificadores, modos e nomes necessários à interface; não inclui conteúdo dos mapas.
