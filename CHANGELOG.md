# Evolução do projeto

Registro organizado a partir do trabalho de 7 e 8 de novembro de 2026. Esta importação preserva o estado atual do código; não fabrica commits retroativos.

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
