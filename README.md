# BF4 Central

Para empacotar a Central e levar o projeto para outro computador, veja [distribuição e reinstalação](docs/DISTRIBUICAO.md).

Central local para Windows que reúne início do jogo, perfis de bots e administração do servidor de Battlefield 4. O ponto de entrada é um EXE próprio; os componentes do jogo e do mod continuam na instalação local.

## Funcionalidades

- Abas Jogo, Bots e desempenho, Servidor e Amigos na rede local.
- Início coordenado de backend, servidor e cliente, reaproveitando processos existentes.
- Identificação do conflito da porta 3216 com o EA App. Somente processos conhecidos e com caminho conferido podem ser encerrados.
- Início do cliente com -blazeIP 127.0.0.1 -patchSSL e verificação da conexão TCP do processo do jogo ao backend local.
- Cinco perfis comportamentais: Noob, Fácil, Normal, Difícil e Extremo. Precisão e cadência das balas permanecem no padrão do mod.
- Modo leve, redução de logs e rotinas cosméticas e limite de quadros no arquivo local do jogo.
- Cliente RCON com autenticação por desafio/hash, catálogo de mapas e edição de rotação.
- Gravação local atômica da rotação com backup e recarga, como alternativa ao mapList.add que falhou na versão examinada.
- Seleção única entre as listas, alvo explícito e botão dedicado a Operation Locker.
- Jogo imediato com um único mapa na rotação, evitando requisitos de expansão vindos de mapas não escolhidos.

## Amigos na mesma rede

Para bots, consulte a [lista de mapas compatíveis](docs/MAPAS-BOTS.md). Caspian Border em Conquista não está no suporte declarado deste mod; a lista de mapas agora prioriza os compatíveis e avisa antes de carregar os demais.

A aba **Amigos • rede local** prepara o anfitrião, exporta/importa convites e mantém até cinco jogadores no mesmo time/pelotão enquanto a Central do anfitrião está aberta. A mudança de pelotão pode aguardar renascimento. Consulte [instruções e limites da rede local](docs/AMIGOS-LAN.md). O fluxo ainda requer validação de entrada com dois computadores.

## Compilar o EXE

### Miniaturas locais

Para embutir também o banner do topo, coloque a imagem escolhida em `local-assets/banner.jpg` antes de executar o empacotador. O banner e os JPGs dos mapas continuam fora do Git. As abas usam ícones próprios desenhados pelo aplicativo e mantêm navegação nativa por teclado.

A seleção de mapas possui miniaturas e uma prévia maior do alvo selecionado. A compilação local pode embutir capas em um único EXE. Coloque JPGs com identificadores de mapa (por exemplo, `MP_Prison.jpg`) em `local-assets/map-images/`, execute `scripts/pack-map-art.ps1` e compile normalmente. `local-assets/` fica fora do Git. As imagens do jogo não são distribuídas neste repositório. Sem esse pacote, o mesmo código compila e mostra placeholders legíveis; a navegação continua funcionando. O EXE não precisa do backend ou da internet para exibir imagens já embutidas.

### Compilação

Requer Windows x64 e compilador do .NET Framework 4.x. Não usa pacotes NuGet.

    .\build.cmd

Saída: dist/BF4 Central.exe. O EXE final solicita elevação para gerenciar a instalação em Program Files. Se scripts estiverem bloqueados por uma política administrada, use o procedimento permitido pela sua administração; o script de compilação mostra a chamada ao compilador.

## Testes isolados

    .\test.cmd

Os testes usam configuração fictícia, servidor RCON simulado e uma raiz temporária; não precisam do jogo nem alteram a instalação real. Os resultados e capturas de renderização são gerados em tests/results. O workflow do GitHub compila no Windows e disponibiliza o EXE como artefato.

## Uso com a instalação local

A Central detecta a pasta ao lado do EXE, a última pasta escolhida e bibliotecas Steam (incluindo `libraryfolders.vdf` e outros discos). Se não encontrar um cliente compatível, abre **Localizar instalação** para escolher a pasta e mostrar os componentes ausentes. Use `--choose-game` para escolher outra pasta. A edição padrão da Steam não é convertida automaticamente para este mod offline.

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
