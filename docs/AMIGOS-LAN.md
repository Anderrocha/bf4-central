# Amigos na rede local

O anfitrião e os convidados precisam estar na mesma rede, ter o BF4/mod compatível instalado e usar nomes distintos. Não é necessário encaminhar portas do roteador. Este fluxo não instala o jogo nem habilita multiplayer oficial da EA.

## Anfitrião

1. Feche o jogo e abra a Central atualizada como administrador.
2. Na aba **Amigos • rede local**, escolha o IP da interface usada pelo amigo e clique em **Preparar rede local**. Isso grava `ServerIP` no backend com backup e reinicia backend/servidor. A rede do Windows deve estar como Privada.
3. Informe seu nome exato e salve o convite. Envie o `.bf4invite` e o EXE da Central ao amigo. O convite contém IPv4 privado, nome do anfitrião, versão e validade de 24 horas. Não contém senha RCON, arquivos do jogo nem um token de autenticação; não restringe quem pode acessar o backend na LAN.
4. Informe até quatro nomes exatos separados por ponto e vírgula e marque **Manter o grupo reunido**. Abra o jogo, entre no servidor e escolha um time/pelotão com espaço.

## Convidado

1. Feche o BF4, abra a mesma Central e escolha **Abrir convite**.
2. Confira o IP/nome do anfitrião, informe um nome diferente e clique em **Aceitar e abrir o jogo**.
3. A Central testa TCP 10040, faz backup da configuração, usa o backend do anfitrião e abre o jogo. O convidado não inicia outro servidor local.
4. No BF4, abra **Multiplayer → Servidores** e entre no servidor do anfitrião. O convite não automatiza o clique de entrada dentro do jogo.

## Pelotão

O anfitrião mantém a Central aberta. A cada 10 segundos, ela consulta `admin.listPlayers all` por RCON local, segue o time e pelotão atual do anfitrião e usa `admin.movePlayer` com `forceKill=false` apenas nos nomes informados. Se ele estiver sem pelotão, escolhe um vazio. Confirma o resultado com nova leitura. Pode aguardar morte/renascimento; não causa morte forçada. O grupo tem no máximo cinco pessoas, não desloca outros jogadores para abrir vagas e não declara sucesso quando o servidor ainda não confirmou. Nomes ausentes aguardam entrada; três falhas consecutivas pausam o monitoramento. O monitoramento termina ao fechar a Central e precisa ser ativado novamente na próxima sessão.

Referência do cliente RCON: [vu-rcon: playerMove/getPlayers](https://multivit4min.github.io/vu-rcon/classes/battlefield.html#playermove).

## Rede e recuperação

As regras adicionadas têm nomes próprios por instalação, limitam-se aos executáveis do backend/servidor, perfil Privado e `LocalSubnet`. Liberam TCP nas portas configuradas do backend (cliente/HTTP/HTTPS/XML/redirecionamento) e UDP apenas na `GAMEPORT` do script instalado. Não liberam a API de início nem a porta RCON para amigos. Não desativam o firewall nem mudam o perfil da rede. Regras anteriores mais amplas não são alteradas.

Backups das configurações ficam em `%LOCALAPPDATA%\BF4BotLauncher\settings-backups`. Se uma etapa falhar após salvar a configuração, os passos anteriores podem já ter sido aplicados; corrija a causa e execute Preparar rede local novamente. Para voltar a jogar sozinho, use BlazeIP `127.0.0.1` na aba Jogo; o backend também pode voltar a `ServerIP=127.0.0.1` usando seu backup. As duas regras `BF4Central-LAN-<instalação>-0/1` podem ser removidas no Firewall Avançado do Windows.

Wi-Fi de convidados/isolamento de clientes pode impedir a conexão. A validação de conectividade do convite é TCP, não prova funcionamento do tráfego UDP do jogo. IP do anfitrião pode mudar ao reconectar a rede; prepare novamente e gere novo convite.

## Validação

Testes isolados cobrem convite, expiração, rejeição de destinos públicos/loopback, formato da lista de jogadores, capacidade, ausência de jogadores, idempotência, troca de time/pelotão e confirmação por leitura. A leitura real de jogadores pelo RCON desta instalação foi validada sem mover ninguém. A entrada e o anúncio do endereço LAN ainda precisam de validação com um segundo computador compatível; não foram comprovados apenas pelos testes simulados.
