# Arquitetura

## Componentes

- Central.cs: janela unificada, fluxo de escolha do mapa e administração.
- Launcher.cs: perfis de bots, backup de INI, temas e editor de comportamento.
- OneClick.cs: coordenação de início, propriedade de portas, API local e comandos do cliente.
- LocalServices.cs: ciclo de vida e janelas dos auxiliares conhecidos, entrada encaminhada ao Battlelog.
- OfflineConnection.cs: observação da conexão TCP do processo do jogo ao backend.
- ServerControl.cs: RCON, serialização de pacotes, catálogo e configuração.
- LocalRotation.cs: persistência atômica e recuperação da lista de mapas local.
- MapNow.cs: escolha, troca e confirmação do mapa/modo/nome.
- LockerRepair.cs: isolamento de Locker e renovação do anúncio do servidor.
- Performance.cs: plano e aplicação reversível de ajustes locais.
- UnifiedTests.cs e RotationMock.cs: testes de interface, protocolo e servidor simulado.

## Dependências externas à Central

O jogo, BF4Launcher.exe original, BF4BlazeServer.exe, servidor dedicado e DLL do mod continuam na pasta de instalação. O EXE próprio não substitui o motor do jogo nem distribui esses arquivos. A compatibilidade do hook é conferida por SHA-256 no código.

## Portas padrão

- 3216: integração local LSX do launcher.
- 32170: API local de início.
- 42190: API local de estado.
- 10040: backend para o cliente do jogo.
- 10041: backend para o servidor dedicado.
- 25210: RCON do servidor local.

As configurações da instalação podem definir outros valores. Chamadas da API de início são restritas ao loopback. Não se repete um POST de início quando seu resultado é incerto.

## Persistência

As alterações locais recebem backup. As credenciais permanecem na configuração local e são mascaradas na interface. A lista de rotação é conferida antes e depois de salvar; falhas tentam recuperar o estado anterior. Nenhum botão encerra processos de caminho desconhecido.

A edição avançada e o jogo imediato têm efeitos diferentes: a primeira prepara uma lista; o segundo usa apenas o mapa escolhido e pode interromper a rodada. O código não garante que todo mapa do catálogo tenha navegação de bots funcional.
