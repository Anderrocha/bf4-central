# Compatibilidade de mapas e bots

O catálogo de 34 mapas identifica conteúdo do BF4; não declara navegação de IA para todos eles. O manual instalado `_说明.txt` do **Battlefield 4 LAN BOT ALPHA v0.2**, datado de 2026-11-07, limita explicitamente os mapas de Conquista. O changelog instalado também descreve cinco novos mapas e a remoção do sistema antigo de logs. Por isso o arquivo antigo `dinput8_bf4hook.log` não foi tratado como diagnóstico da sessão atual.

## Conquista grande: suporte declarado

- Operation Locker (`MP_Prison`)
- Hainan Resort (`MP_Resort`)
- Zavod 311 (`MP_Abandoned`)
- Zavod: Graveyard Shift (`XP5_Night_01`)
- Siege of Shanghai (`MP_Siege`)
- Propaganda (`XP3_Prpganda`)
- Operation Metro 2014 (`XP0_Metro`)
- Pearl Market (`XP3_MarketPl`)
- Dawnbreaker (`MP_Tremors`)
- Guilin Peaks (`XP1_003`)
- Rogue Transmission (`MP_TheDish`)
- Paracel Storm (`MP_Naval`)

O manual também declara suporte ao modo Superioridade aérea; a Central o aplica somente aos mapas que o catálogo oferece nesse modo. Não presume suporte a Conquista pequena, Investida ou outros modos a partir da declaração de Conquista. Suporte declarado não significa teste de jogabilidade ou ausência de falhas em cada mapa.

## Caspian Border 2014

Caspian (`XP0_Caspian`) não consta na lista de Conquista suportada. Na sessão examinada, RCON confirmou `XP0_Caspian / ConquestLarge0`, 33 participantes e estado `IN_GAME`. Isso comprova mapa/participantes, mas não bots ativos. As imagens do usuário mostram o sintoma e o manual oferece a explicação mais forte: faltam garantias de navegação/controle de IA para esse mapa nesta versão. Não houve diagnóstico atual comprovando a falha interna exata do grafo.

Mudar quantidade, dificuldade ou gráficos não acrescenta navegação a um mapa. Para habilitar bots funcionais em Caspian seria necessário um mod/pacote de navegação que explicitamente o suporte, junto de teste real. Não foi modificado nem substituído o pacote binário instalado.

## Comportamento da Central

Na aba Servidor, a lista mostra inicialmente somente mapas com suporte declarado para o modo selecionado. **Mostrar todos (inclui mapas sem bots)** reabre o catálogo completo, marcando os demais. Carregar um mapa/modo sem suporte confirmado exibe um aviso antes de alterar a partida. Rotações antigas continuam visíveis; não são apagadas nem substituídas silenciosamente. Nenhum mapa em andamento é trocado apenas por abrir a Central.
