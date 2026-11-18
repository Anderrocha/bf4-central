# Evolução do projeto

Registro organizado a partir do trabalho de 7 e 8 de novembro de 2026. Esta importação preserva o estado atual do código; não fabrica commits retroativos.

## Controle remoto do servidor (18/11/2026) — painel 0.5.21
- O servidor do BF4 só aceita o RCON (25210) de dentro do próprio PC; a Central do PC de jogo ficava presa em "Conectando ao servidor por RCON".
- Painel agora repassa o RCON: escuta na TCP 25212 da rede e encaminha para 127.0.0.1:25210. O "pronto" do pedido devolve a porta 25212, que a Central grava como RconPort. Regra de firewall nova (versão v3).

## Painel sempre ligado (18/11/2026) — 0.5.20
- Painel BF4 Servidor: na primeira vez que abre (UAC uma vez só) se instala como "Sempre ligado (24 h)": tarefa agendada com gatilho no logon e a cada 1 minuto (uma cópia só), então volta sozinho após reinício, queda de energia ou crash, e fica perto do relógio só esperando pedidos.
- O estado "ligado a pedido" fica salvo em arquivo: desconectar pela Central desliga o servidor mesmo se o painel caiu no meio.
- Central: o botão vira "Desconectar do PC servidor" enquanto conectado (desliga lá e volta tudo para este PC).

## PC servidor com pedido e aceite (18/11/2026) — 0.5.19
- Central: botão "Conectar ao PC servidor" (aba Servidor). Acha o painel na rede sozinho (último IP usado ou busca UDP 25231), pede pela porta TCP 25230 e espera o dono aceitar.
- Aceitou: o outro PC liga o servidor; a Central fecha o servidor e o backend locais, aponta Blaze e RCON para o outro PC e abre o jogo (todos os núcleos ficam para o jogo).
- Recusou, não respondeu em 60 s ou o painel está fechado: tudo volta para o servidor deste PC, com a dica de usar menos bots e mapas fechados.
- "Voltar ao meu servidor" também libera o PC servidor (ele desliga o que ligou a pedido). "Iniciar tudo e jogar" volta para este PC se o PC servidor não estiver mais ligado.
- Painel BF4 Servidor redesenhado no visual da Central (cabeçalho com a arte, cartões, interruptores). Janela de pedido com "Aceitar e ligar", "Recusar", "Sempre aceitar deste PC" e recusa automática em 60 s.
- Painel: fica perto do relógio ao fechar, opção "Abrir com o Windows" (tarefa agendada no logon) e regras de firewall novas (porta RCON 25210, pedidos 25230/25231).

## Servidor em outro PC: painel "BF4 Servidor" (18/11/2026) — 0.5.18

- Novo programa `servidor\ServidorPainel.cs` (`dist\BF4 Servidor.exe`, `servidor\build.cmd`): painel para um segundo PC da rede com um botão Ligar/Desligar. No primeiro uso libera o backend e o servidor no firewall (rede privada); a cada ligação grava o IP do PC no `Server\config.txt` (ServerIP), liga o backend, depois o servidor com bots (parâmetros lidos do `!StartServer.bat`), tudo sem janelas, e mostra o IP para usar na Central. Opção "Modo leve" (prioridade baixa).
- Central: sem servidor local aberto (servidor em outro PC), "Separar núcleos" não limita o jogo, que fica com todos os núcleos.

## Servidor com a mesma prioridade do jogo (18/11/2026) — 0.5.17

- Com "Separar núcleos" ligado, o servidor (bf4_Server_Final: acertos, kills e IA dos bots) passa a ter prioridade acima do normal, igual ao jogo. Antes ele ficava em prioridade normal e perdia o núcleo 3 para o jogo, ficando quase só no núcleo 4: o kill chegava atrasado e os mapas abertos atrasavam. Medido: o servidor ocupa 99% de um núcleo mesmo sem partida.

## Jogo achado também pela EA App e Origin (17/11/2026) — 0.5.16

- A busca da pasta do jogo agora lê o "Install Dir" que a EA App e a Origin gravam no registro (EA Games\Battlefield 4) e procura pastas comuns em todos os discos (Program Files\EA Games, Origin Games, EA Games, Games, Jogos), além das bibliotecas da Steam. Antes, fora da Steam, só a pasta padrão C:\Program Files\EA Games era tentada.

## Aviso quando outra Central está aberta (17/11/2026) — 0.5.15

- Se outra BF4 Central já estiver aberta para a mesma instalação do jogo (por exemplo, uma versão antiga em outra pasta), a nova mostra a versão e o caminho da outra e oferece fechá-la e abrir a nova. Antes, a nova fechava em silêncio e a antiga continuava na tela.
- Pacote completo: o instalador fecha Centrais abertas, corrige atalhos que apontam para cópias antigas, avisa sobre cópias soltas e confere a versão instalada.

## Um som por vez entre a aba Servidor e a tela final (17/11/2026) — 0.5.14

- Ir para a tela final para a música do mapa antes da intro; voltar para a aba Servidor para a intro e retoma a música do mapa escolhido. Antes as duas tocavam juntas.

## Intro do BF4 no banner da tela final (17/11/2026) — 0.5.13

- Ao abrir a tela "Tudo pronto para jogar", o banner toca a intro do Battlefield 4 (15 s, a partir do fim dos logos), com som, e para no último quadro: a arte do Battlefield 4. O som segue a caixa "Música do mapa".
- Quadros (24 por segundo) e som ficam em `%LOCALAPPDATA%\BF4BotLauncher\intro\` (`intro.bin` + `intro.mp3`), gerados neste PC a partir do vídeo do usuário. Não vão para o repositório; sem a pasta, o banner continua com a imagem fixa.

## Música de abertura de cada mapa (17/11/2026) — 0.5.13

- Aba Servidor: ao escolher um mapa na lista (ou na rotação), toca a música de abertura daquele mapa; trocar de mapa troca a música. Caixa "Música do mapa" liga/desliga (salva em `%LOCALAPPDATA%\BF4BotLauncher\map-music.txt`). A música para ao sair da aba Servidor, ao clicar em "Iniciar tudo e jogar" e ao fechar a Central.
- As músicas são lidas de `%LOCALAPPDATA%\BF4BotLauncher\music\<IdDoMapa>.mp3`, extraídas do jogo instalado neste PC (faixas `..._music_loading_..._wave` dos pacotes de cada mapa, convertidas com vgmstream e ffmpeg). Por serem conteúdo da EA, não ficam no repositório. Sem a pasta, a caixa fica desativada.

## Etapas somem ao sair da partida (15/11/2026) — 0.5.12

- Depois de "Iniciar tudo e jogar" terminar com "Conectado", a Central confere a cada 5 s se o BF4 continua aberto e se o nome do jogador está na lista de jogadores do servidor (RCON `admin.listPlayers`).
- Saiu da partida (depois de ter sido visto nela): as etapas saem da barra e aparece "Você saiu da partida…". Fechou o jogo: as etapas saem e aparece "O Battlefield 4 foi fechado…".
- Sem RCON disponível, só o fechamento do jogo é detectado. Se outra operação trocar as etapas (ex.: carregar mapa), o acompanhamento para.

## Cartão do Sócio Premiado na tela final (15/11/2026) — 0.5.11

- Tela "Tudo pronto para jogar": cartão com a imagem do projeto Sócio Premiado, o convite "Ajude o projeto a evoluir: participe e concorra a prêmios.", o link `sociopremiado.com.br` e o botão "Ir para o site" (imagem, link e botão abrem https://sociopremiado.com.br/ no navegador). Imagem embutida em `resources\socio-premiado.png`. O banner da tela final ficou mais baixo para caber o cartão.

## Botão "Abrir Battlelog" (15/11/2026) — 0.5.10

- Aba Servidor: novo botão "Abrir Battlelog", ao lado de "Salvar conexão". Abre no navegador o Battlelog local do backend do mod (`http://127.0.0.1/bf4/en/`, com menu Soldier: estatísticas, Loadout/Customize, unlocks, medalhas, battlepacks; porta `HttpPort` de `Server\config.txt`). Só fica ativo quando o `BF4BlazeServer` desta instalação está rodando e escutando a porta (conferido a cada 3 s). Login: nome do jogador, senha em branco.

## Jogo ainda carregando não dá mais erro (14/11/2026) — 0.5.9

- Com o BF4 já aberto há menos de 5 minutos e ainda sem conexão com o backend (carregando), a Central espera até 120 s em vez de mostrar "O jogo aberto não está conectado ao backend offline".
- Com o jogo já aberto, a Central também aplica a separação de núcleos e a tela inteira.

## Núcleos separados entre jogo e bots (14/11/2026) — 0.5.8

- Nova chave na tela final: "Separar núcleos: jogo × bots" (padrão ligada). Com 4 núcleos, o jogo fica nos núcleos 1-3 com prioridade acima do normal, o servidor (IA dos bots) nos núcleos 3-4 e o backend no núcleo 4, com prioridade abaixo do normal. Desligada, tudo volta a usar todos os núcleos com prioridade normal no próximo início. Vale a partir de "Iniciar tudo e jogar". Salva em `%LOCALAPPDATA%\BF4BotLauncher\split-cores.txt`.
- Cache de mapa: não há ganho em pré-carregar mapas. O conteúdo é compartilhado em ~61 GB de pacotes .cas e o Windows já mantém em RAM o que foi lido. O cache de shaders da NVIDIA já está ativo.

## Chave "Entrar direto na partida" na tela final (14/11/2026) — 0.5.7

- Na tela "Tudo pronto para jogar", uma chave liga/desliga a entrada direta no servidor. Desligada, o BF4 abre na tela de boas-vindas como antes. A escolha fica salva em `%LOCALAPPDATA%\BF4BotLauncher\direct-join.txt` (padrão: ligada).

## Aba Bots: perfil salvo e perfil ativo visíveis (14/11/2026) — 0.5.6

- A aba abre no perfil que está salvo no `dinput8_bf4hook.ini` (antes abria sempre em "Normal", mesmo com outro salvo).
- O botão do perfil salvo ganha borda verde. A linha de baixo acompanha o botão escolhido: "Difícil • configurado e ativo na partida ✓", "Fácil • não salvo ainda • em uso: Difícil" ou "… salvo; reinicie o servidor para valer na partida".
- "Ativo na partida" compara a hora em que o servidor aberto iniciou com a última gravação do .ini (a IA dos bots lê o arquivo ao iniciar o servidor). Atualiza a cada 5 s.
## Entrar direto no servidor e "Conectado" corrigido (14/11/2026) — 0.5.5

- "Iniciar tudo e jogar" agora entra direto no servidor local, sem clicar em "Servidor recente": a Central entra no Battlelog local do backend com o jogador da Central, espera o servidor aparecer na lista (até 90 s), pede a reserva de vaga (`/launcher/prepare`, o mesmo fluxo do botão do Battlelog) e abre o jogo com `-requestState State_ConnectToGameId`. Se algo falhar (login com senha, servidor fora da lista), cai no início normal pelo menu.
- Etapa "Conectado" corrigida: o backend entrega ao jogo o `ServerIP` da rede (ex.: 192.168.0.8) e a Central só aceitava 127.0.0.1, então a etapa terminava em ✕ mesmo com o jogo conectado. Agora qualquer IP deste PC conta como backend local; espera até 120 s.
- Tela inteira: a Central não restaura mais a janela do jogo ao focar (isso podia tirar o BF4 da tela inteira). Se o perfil pede tela inteira (`GstRender.FullscreenEnabled 1`) e o jogo aparecer em janela, a Central alterna com Alt+Enter.
## Idiomas: português, inglês e espanhol (13/11/2026) — 0.5.4

- Seletor de idioma com bandeira no cabeçalho (Brasil, Estados Unidos, Espanha). A escolha fica salva em `%LOCALAPPDATA%\BF4BotLauncher\language.txt`; sem escolha, segue o idioma do Windows.
- Tradução feita na exibição (`src\Language.cs` + dicionário em `src\Translations.cs`): os textos de origem continuam em português, então o acompanhamento de etapas ✓ não muda. Mensagens montadas em partes são traduzidas por trechos.
- Seletor próprio no estilo dos botões: pílula arredondada com cores para mouse por cima, clique (âmbar) e aberto; seta que gira; menu arredondado com destaque ao passar o mouse e idioma atual em âmbar com ✓. Também funciona pelo teclado (setas, Enter, Esc).
- A troca é ao vivo, sem reiniciar: abas, botões, barra de etapas, prévia do mapa, listas, diálogos de confirmação e a janela de localizar instalação.
- Botões em fileira se alargam quando a tradução não cabe; colunas da conexão RCON ajustadas para "Puerto"/"Contraseña".
- Teste novo (`LanguageTests`): troca PT→EN→ES→PT pela interface e gera prints `central-en-*.png`, `central-es-*.png` e `language-flags.png`.
## Refinamento visual (13/11/2026)

- Botões próprios (`ActionButton`) com cantos arredondados e cores distintas para mouse por cima, clique, em andamento (âmbar com indicador girando), concluído (verde ✓) e falha (vermelho ✕).
- A barra inferior virou um acompanhamento de etapas (`StatusBar`/`StepTracker`): Iniciar tudo, Iniciar servidor local e Carregar mapa mostram cada etapa, e o ✓ só aparece quando a etapa seguinte começa ou a operação termina com sucesso. Em caso de falha, a etapa ativa fica marcada com ✕ e as seguintes continuam pendentes.
- A tela final "Tudo pronto para jogar" mostra as mesmas etapas durante o início.
- A troca de mapa agora informa a conexão RCON, a gravação da rotação e a confirmação do mapa.
- Correções de layout: a primeira fileira da aba Servidor não fica mais cortada, o texto "Adicionar →" não é truncado e a prévia do mapa se adapta ao tamanho mínimo da janela.

## Central unificada e prioridade de Operation Locker

- Interface única com jogo, bots e gerenciamento RCON.
- Nome do servidor acompanha o mapa escolhido.
- Seleção única entre catálogo e rotação, com alvo explícito.
- Locker é o primeiro mapa e tem botão dedicado.
- Jogo imediato usa rotação exclusiva; a lista e rascunhos anteriores recebem backup.
- Reinício de reparação renova o anúncio do servidor com somente Locker.
- O aviso de expansão foi relacionado aos requisitos anunciados pela rotação completa. A remoção do aviso na entrada efetiva da partida ainda precisa de confirmação.

## Mapas e rotação

- Cliente RCON próprio com desafio/hash e limites de pacote.
- Catálogo com 34 identificadores de mapas e seus modos.
- Edição de rodadas, inclusão, remoção e ordenação.
- Detecção de lista desatualizada, backup, leitura de confirmação e recuperação.
- Teste real revelou adições ignoradas por mapList.add. O caminho local passou a escrever MapList.txt atomicamente e executar mapList.load.
- Runtime anterior restaurado e conferido quando o teste de reconstrução por RCON falhou.

## Início offline em um clique

- Backend, servidor e launcher original coordenados em segundo plano.
- Verificação de propriedade das portas e caminhos dos processos.
- Reuso de processos, serialização do início e esperas limitadas.
- Conflito com EA App na porta 3216 tratado sem encerrar programas desconhecidos.
- Corrigido o envio à API: o cliente precisa receber -blazeIP e -patchSSL além dos argumentos extras.
- Conexão local observada no processo do jogo, sem tratar apenas o estado running como sessão conectada.

## Bots e desempenho

- Cinco presets comportamentais; não alteram a precisão ou cadência de tiros que o mod não expõe.
- Quantidade de bots separada da dificuldade.
- Modo leve reduz chats cosméticos, granadas e frequência de suprimentos.
- Redução de logs e telemetria configurável e limite de 60 FPS.
- Dois servidores simultâneos foram identificados e o início passou a evitar duplicação.
- Perfil gráfico local da GTX 1050 foi ajustado com backup. Esse perfil pessoal e seus arquivos não foram incluídos no repositório.

## Publicação

- Código atual, manifests, catálogo factual, documentação e testes isolados.
- Build Windows sem arquivos do jogo e workflow para gerar artefatos.
- Configurações reais, credenciais, perfis, binários de terceiros e logs locais excluídos.
# Amigos LAN — 2026-11-08

- Aba Amigos com preparo do anfitrião, convite de 24 horas e conexão do convidado ao backend LAN.
- Regras de firewall por programa, rede Privada/sub-rede local; RCON e APIs administrativas não são compartilhados.
- Grupo de até cinco nomes acompanha time/pelotão do anfitrião por RCON, sem morte forçada, com verificação, capacidade e pausa após falhas.
- Conexão TCP do cliente também é verificada para backend remoto.
- Testes de convites, validação, mudança de time/pelotão, espera, capacidade e confirmação; teste de dois computadores continua pendente.

# Instalação Steam — 2026-11-08

- Detecção do diretório do EXE, pasta lembrada, bibliotecas Steam e bibliotecas de outros discos.
- Seleção e diagnóstico de pasta quando a instalação não é encontrada; opção `--choose-game`.
- Convidados compatíveis deixam de exigir os arquivos exclusivos de hospedagem do servidor.
- Testes de bibliotecas VDF, cliente completo/incompleto e convidado sem servidor local.

# Compatibilidade de bots — 2026-11-08

- Separar o catálogo do jogo da declaração de suporte do mod LAN BOT ALPHA v0.2.
- Mostrar mapas compatíveis por padrão, oferecer o catálogo completo e avisar antes de carregar mapas/modos não confirmados.
- Identificar Caspian Conquista como não confirmado; preservar o mapa atual e rotações existentes.
- Testes de política e do filtro da lista. Nenhuma alegação de navegação corrigida em Caspian.

# Miniaturas e prévia — 2026-11-08

- Miniaturas de 96×48 na lista de mapas e capa maior acompanhando a seleção de ambas as listas.
- Nome, modo e suporte de bots legíveis sobre a prévia; compatibilidade substitui o identificador técnico na lista principal.
- Pacote local opcional de imagens embutido no EXE; código compila sem imagens e usa fallback.
- Capas de 33 mapas localizadas no pacote HTTP já instalado, via backend local. MP_Playground não tem capa disponível. Recursos de jogo não enviados ao GitHub.
- Testes de seleção/prévia, imagem ausente e renderização com e sem pacote local.
# 0.4.1 — 2026-11-08

- Banner com a imagem fornecida pelo usuário, incorporado ao pacote de recursos local.
- Abas maiores com ícones próprios, texto em negrito, seleção escura e sublinhado âmbar.
- Foco de teclado nativo e layout conferido no tamanho mínimo; navegação e operações preservadas.

# 0.5.0 — Assistente de preparação

- Cinco etapas com Voltar, Próximo, progresso persistente por instalação e ação de jogar após a validação final.
- Instalação do mod a partir do pacote local com integridade, backup e script embutido.
- Detecção de jogo base ainda sem mod e atalho para convidados com cliente já preparado.
- Testes isolados de bloqueio sem componentes e avanço de 0 a 100%.

# 0.5.1 — Etapas simplificadas

- Instalação valida automaticamente; atalhos de verificação manual e convite foram removidos da primeira etapa.
- Jogo apresenta somente as opções de conexão e identidade, preservando os argumentos previamente salvos.
- Modo leve tem seletor visual com estado textual e teclado; Bots não reinicia servidor nem inicia o jogo.
- Servidor usa Adicionar à lista; carregar mapa selecionado não abre o cliente.
- Tela final com banner ampliado, ação única de jogar e textos de início vinculados ao progresso real do launcher.

