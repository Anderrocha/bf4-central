# Validação registrada

## Testes executados no desenvolvimento

- Codificação/decodificação RCON e UTF-8.
- Rejeição de pacotes truncados e verificação de cabeçalho/sequência.
- Catálogo de 34 mapas com prioridade de Locker.
- Rotação imediata com apenas Locker, sem mapas XP adicionais.
- Gravação e leitura, conflito de edição, troca de mapa e nome.
- Reuso de mapa já ativo sem reiniciar a rodada desnecessariamente.
- Falhas simuladas com restauração de arquivo e runtime.
- Seleção única entre as duas listas.
- Renderização das três abas em dois tamanhos.

## Evidência real local

Foi observada autenticação RCON e leitura da lista ativa. O jogo foi observado conectado ao backend em loopback e o servidor apareceu no menu do multiplayer. Operation Locker foi confirmado por serverInfo como MP_Prison / ConquestLarge0, com o nome Offline - Operation Locker.

O relatório local posterior confirmou rotação persistida e ativa com uma única entrada MP_Prison ConquestLarge0 1 e reinício do servidor. Logs e capturas pessoais originais não são publicados aqui.

Não foi concluída a validação da ausência do aviso de expansão ao entrar efetivamente na partida. Essa pendência não deve ser tratada como resolvida apenas porque RCON ou TCP responderam.

## Reproduzir testes sem o jogo

Execute scripts/test.ps1. O script usa configuração fictícia, catálogo embutido e uma pasta temporária indicada por BF4_CENTRAL_TEST_ROOT. Não executa os modos de teste real, que podem alterar ou reiniciar um servidor.
