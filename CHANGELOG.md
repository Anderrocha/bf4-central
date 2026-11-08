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
