using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
namespace BF4Bots {
// Chat dos bots em português, feito pela Central (19/11/2026; ampliado em 21/11/2026).
// As falas do mod ficam embutidas e criptografadas na dinput8.dll (só inglês/russo/pinyin), então a Central
// desliga o chat do mod e escreve as falas pelo RCON (admin.say), reagindo aos eventos do servidor
// (player.onKill, player.onChat, server.onRoundOver). A mensagem sai como aviso do servidor com o nome do bot no texto.
// 0.5.32: muito mais frases, sem repetir as recentes, fluxo maior, denúncias ("reporta", "IA anticheat"),
// votação de kick (vote 1 / vote 2), chamadas táticas, placar, elogio ao jogador e canal de time/esquadrão
// para o time do jogador (o jogo pinta TIME e ESQUADRÃO com cores próprias; o time inimigo fala no geral).
// Sem acentos de propósito: o chat do BF4 nem sempre mostra acentos e o pessoal digita assim mesmo.
public static class ChatPhrases {
 // {k} = quem matou, {v} = quem morreu, {w} = arma, {n} = sequência, {h} = jogador de verdade citado,
 // {t} = alvo da votação/denúncia, {f} = bandeira, {y}/{no} = votos sim/não
 public static readonly string[] Rage={
  "puta que pariu, de novo?","caralho, nao consigo nem nascer direito","porra {k}, me deixa em paz","{n} mortes seguidas, que desgraca","vou desinstalar essa merda",
  "{k} filho da puta, ta me cacando?","pqp, nasci e ja morri","alguem mata esse {k} pelo amor de deus","cacete, que fase","to tomando mais tiro que alvo de treino",
  "nao e possivel, de novo esse arrombado","{k} vai tomar no cu","essa porra de spawn ta amaldicoada","desisto, vou jogar paciencia","morri {n} vezes sem dar um tiro, que lixo",
  "ta de sacanagem comigo, {k}","eu juro que ele ta de hack","meu deus, que dia de merda","de novo nao, porra","quem me colocou nesse time de bosta?",
  "sera que da pra parar de me matar, {k}?","vtnc {k}","to virando estatistica aqui","caralho, me pegou ate dormindo","vou ali chorar e ja volto",
  "{k} de novo? casa comigo entao","nem deu tempo de mirar","ja e a {n}a vez, {k}, para","meu k/d ta chorando","nasci, pisquei, morri",
  "spawn kill de novo, que servidor e esse","{k} campeando la atras, covarde","alguem me explica como ele me viu","eu tava atras da parede, porra","to so de enfeite nesse mapa",
  "chega, vou trocar de classe","{k} tem mira de robo","assim nao da, assim nao pode","to perdendo pro meu proprio mouse","quem me matou? ah, o {k} de novo",
  "pelo menos me deixa chegar na bandeira, {k}","{n} seguidas, vou pedir musica no fantastico","que fase, meus amigos","o {k} ta morando na minha cabeca","respawn so pra morrer, que beleza"};
 public static readonly string[] Taunt={
  "senta, {v}","chora {v} kkkkk","vai pro lobby, {v}","foi mal {v}, nao foi nada pessoal... mentira, foi sim","de nada pelo respawn, {v}",
  "{v}, tenta de novo que ta facil","kkkkkkk tomou {v}","bota no replay essa, {v}","mais um pra conta","{v} veio de presente",
  "achou que ia onde, {v}?","volta pro tutorial, {v}","{v} caiu igual pato","pegou o {w} na cara, {v}","pode reclamar com o bispo, {v}",
  "toma, {v}, e leva pra casa","{v} nem viu de onde veio kkk","ez","essa foi bonita, admite {v}","{v} correndo em linha reta, ai fica facil",
  "{v} veio sozinho, voltou deitado","obrigado pelos pontos, {v}","{v}, teu kit e meu agora","boa noite, {v}","{v} achou que era o protagonista",
  "nem precisei mirar, {v}","{v}, manda um abraco pro spawn","limpei a {f}, {v} ficou pelo caminho","de {w} e tudo, {v}","{v} tropecou na minha bala",
  "fica ai deitado, {v}, que eu ja volto","mais um {v} pra colecao","e o {v} que tava falando grosso?","{v} sumiu do mapa kkk","gg {v}, so que nao"};
 public static readonly string[] HumanTaunt={
  "e ai {v}, so isso?","{v} achou que bot nao mira kkkkk","o humano caiu, chama o suporte","{v}, ce tava de olho fechado?","toma {v}, bot tambem joga",
  "{v} vai culpar o lag agora","kkkkkk {v} tomou de bot","{v}, quer que eu deixe mais facil?","manda o proximo, {v} ja era","{v} rage quit em 3, 2, 1...",
  "{v}, o bot aqui manda lembranca","humano detectado e eliminado","{v} caiu igual os outros","nao fica bravo nao, {v}","{v}, vai treinar no campo de tiro",
  "o famoso {v} caiu kkkk","{v} achou que era facil","print nessa, matei o {v}","{v}, cade aquela mira toda?","um a zero pra mim, {v}"};
 public static readonly string[] VsHuman={
  "{k} ta jogando muito, porra","caralho {k}, pega leve","{k} ta de hack, certeza","que tiro foi esse, {k}?","{k} filho da puta, que mira",
  "alguem segura esse {k}","{k} vai tomar no cu, serio","respeito, {k}... mas vai se fuder","{k} ta destruindo, chama reforco","mano, o {k} nao erra",
  "o {k} e humano mesmo? ta parecendo hack","{k} me pegou de novo, que odio","foca no {k}, galera","alguem marca o {k} no mapa","{k} ta sozinho matando o time todo",
  "esse {k} nao e normal","{k}, liga a camera ai que eu quero ver","que reflexo e esse, {k}?","odeio esse {k}","{k} amassando, socorro"};
 public static readonly string[] HumanKillPraise={
  "boa {k}!","{k} carregando o time","nice {k}","isso {k}, mata mais um","{k} ta on hoje","valeu {k}, tava me cacando esse ai",
  "{k} monstro","segue o {k} que da bom","que tiro, {k}!","{k} limpando a area","boa, {k}, vamo pra {f}","{k} e o mvp dessa partida"};
 public static readonly string[] Headshot={
  "na cabeca, porra!","headshot, {v} ficou sem tampa","bem no meio da testa","{v} perdeu o capacete e a dignidade","que tiro lindo, nem eu acreditei",
  "boom, headshot","{v} levou na moleira","essa foi na tampa, {v}","de longe e na cabeca, respeita","mira laser hoje",
  "um tiro so, {v}","{v} tomou na testa kkk","headshot de {w}, respeita","pintou a parede com o {v}","to calibrado hoje"};
 public static readonly string[] HeadshotVictim={"na cabeca nao, porra","de onde veio isso caralho","{k} me deu um headshot do outro lado do mapa, que merda","tiro na testa, que vergonha",
  "um tiro e acabou, ta de brincadeira","{k} sniper safado","como que acerta na cabeca dali?","fiquei sem tampa"};
 public static readonly string[] Streak={
  "{n} seguidas, ninguem me para","to on fire, {n} kills","alguem me para? ninguem? beleza","{n} sem morrer, chama o medico pro outro time","que sequencia gostosa, {n}",
  "to sozinho carregando esse time","tragam mais, {n} ja foram","modo deus ativado, {n} kills","{n} e contando","hoje eu to impossivel, {n}"};
 public static readonly string[] Suicide={"quem colocou essa granada ai? ah, fui eu","me matei sozinho, que orgulho","cai feito um saco de batata","morri pra gravidade, que vergonha",
  "minha propria granada, que fase","nao pergunta","pulei achando que dava, nao dava"};
 public static readonly string[] Generic={
  "bora time, segura a bandeira","alguem tem municao ai?","medico! cade a porra do medico?","pega a bandeira {f}, caralho","sniper na colina, cuidado",
  "esse mapa e uma merda pra correr","quem ta no tanque? usa direito pelo amor de deus","to sem kit, alguem joga uma caixa","segura ai que eu to chegando","cade o esquadrao? cada um pra um lado",
  "quem ta com lag ai?","esse servidor ta lotado hoje","alguem viu quem ta ganhando?","to de engenheiro, alguem precisa de reparo?","esse time ta dormindo"};
 public static readonly string[] Callout={
  "bandeira {f} sendo capturada!","cuidado, tanque na {f}","sniper no predio perto da {f}","preciso de medico na {f}","vamos de {f}, ta vazia",
  "to sem municao, alguem joga uma caixa","inimigo flanqueando pela direita","segura a {f}, to indo","granada!","eles tao vindo pela {f}",
  "alguem bota um spawn beacon","perdemos a {f}, bora retomar","dois inimigos na {f}","heli inimigo, alguem com stinger?","to na {f}, vem ajudar",
  "cobre ai que eu vou reviver","atencao, eles tao nascendo na {f}","fumaca na {f}, entra agora","mina na estrada, cuidado com o carro","defende a {f}, eu ataco a outra"};
 public static readonly string[] ToHuman={
  "{h} vem pro meu esquadrao","{h} joga uma caixa ai","{h} cobre a gente na {f}","{h}, segura a {f} que eu vou na outra","bora {h}, vamo junto na {f}",
  "{h} ta de que classe?","{h}, me revive se der","{h}, sniper no teu lado","valeu pela ajuda, {h}","{h}, cuidado que o tanque ta vindo"};
 public static readonly string[] Reply={
  "kkkkkkk","verdade","para de chorar","nem fala","concordo, que merda","foco no jogo, porra","relaxa que a gente vira","kkkk tomou","f","calma ai, mano",
  "acontece","normal","kkkk sempre ele","eu vi isso","nem liga","isso ai","joga e cala a boca kkk","mano kkkkk","respeita","que isso"};
 public static readonly string[] HumanChatReply={
  "fala {h}","{h} ta falando muito pra quem ta morrendo","kkkkk {h}","respeita os bots, {h}","{h}, cala a boca e joga","bora {h}, ajuda o time",
  "nao enche {h}","{h} ce ta de que lado?","olha o {h} se achando","pode crer, {h}","verdade {h}","{h} sabe das coisas","ninguem perguntou, {h} kkkk","tamo junto {h}"};
 public static readonly string[] HumanGg={"gg {h}","gg wp","gg, partida boa","gg, revanche?","gg {h}, jogou muito"};
 public static readonly string[] HumanBot={"bot e a vo, {h}","bot que te mata, ne {h}?","eu sou bot mas tenho sentimentos, {h}","bot sim, ruim nao","bot e quem fala, {h}"};
 public static readonly string[] HumanInsult={"vai tomar no cu voce, {h}","olha a boca, {h}... arrombado","fala isso na minha cara, {h}","{h} ta nervosinho kkkkk","reportado por ofensa, {h} kkkk"};
 public static readonly string[] HumanLaugh={"kkkkkkkkkk","rindo de que, {h}?","kkkk o {h} ta feliz","ri agora, {h}, depois chora"};
 public static readonly string[] HumanHack={"hack nada, {h}, e skill","ta chorando, {h}?","{h} reporta entao kkkk","olha quem fala de hack, {h}"};
 // Denúncias: quem morreu pede report do {k}; outros ecoam; colegas do {k} defendem.
 public static readonly string[] Report={
  "report no {k}, ta de hack","alguem reporta esse {k}","{k} ta de wallhack, me viu atras da parede","admin, olha o {k} pelo amor de deus","denunciei o {k} pra IA do servidor",
  "{k} ta de aimbot, certeza","report {k}, ninguem joga assim","manda o {k} pro banimento","IA, verifica o {k} ai","{k} com mira travada, report",
  "isso e cheat, {k}","reportado, {k}","alguem grava o {k} ai","esse {k} tem que tomar ban","{k} ta vendo atraves da fumaca, report"};
 public static readonly string[] ReportEcho={
  "reportado","ja reportei o {k}","report tambem","{k} ta muito estranho mesmo","eu vi, ele virou de costas e me matou",
  "manda print pro admin","IA do servidor, olha o {k}","denunciei tambem","mais um report pro {k}","+1 report no {k}",
  "{k} nao erra um tiro, suspeito","vamo votar pra kickar o {k}","report report report","{k} ta flutuando ate"};
 public static readonly string[] ReportDefend={
  "chora mais","ele e bom so, aceita","report de que? skill?","kkkk tao chorando","o {k} e o melhor do nosso time, deixa ele",
  "aprende a jogar ao inves de reportar","nao e hack, voces que sao ruins","{k} mito","chorem, chorem"};
 // "IA anticheat" do servidor: começa a análise e libera (é brincadeira, ninguém é expulso).
 public static readonly string[] AiStart={
  "{n} denuncias contra {t}. analisando o jogador...","verificando {t}: mira, movimento e historico de kills...","analise iniciada no jogador {t}. aguarde",
  "recebemos denuncias de {t}. a IA esta assistindo","escaneando {t}..."};
 public static readonly string[] AiClear={
  "analise concluida: {t} esta limpo. e skill mesmo","nenhuma trapaca detectada em {t}. parem de chorar","{t} liberado. sugestao da IA: joguem melhor",
  "{t} esta limpo. denuncias arquivadas","resultado: {t} e so bom. proximo","nada encontrado em {t}. bora jogar"};
 public static readonly string[] AiReact={
  "IA comprada kkkk","ate a IA ta do lado dele","falei que ele era bom","IA cega","kkkkk chorou pra IA e nao deu","ta vendo? skill","impossivel, IA bugada","aceita que doi menos"};
 // Votação de kick.
 public static readonly string[] VoteStart={
  "votekick {t}: digita 1 pra kickar, 2 pra ficar","votacao: kickar o {t}? vote 1 sim, 2 nao","bora votar: 1 kicka o {t}, 2 deixa","vote 1 pra kickar o {t}, 2 pra nao",
  "abri votacao contra o {t}. 1 = kick, 2 = fica"};
 public static readonly string[] VoteStartOwn={
  "votekick {t}, so morre: 1 kicka, 2 fica","{t} ta jogando pro outro time. vote 1 pra kickar","1 pra kickar o {t}, 2 pra dar mais uma chance","votacao: {t} ja morreu {n} vezes. 1 kick, 2 fica"};
 public static readonly string[] VoteYes={"1","1","1","1!!!","1 kkkkk","1, tchau {t}","11111","1 sem do","1 vaza {t}","um","1 pelo amor de deus","1, finalmente"};
 public static readonly string[] VoteNo={"2","2","2, deixa o cara","2 ele e do meu esquadrao","2, aceita que doi menos","2 kkkk","2, ta chorando por que?","dois","2 fica {t}"};
 public static readonly string[] VoteResult={
  "votacao encerrada: {y} sim, {no} nao. minimo de 60% nao atingido, {t} fica","resultado: {y} x {no}. {t} continua no servidor","{y} votos pra kickar, {no} pra ficar. nao passou, {t} fica",
  "votacao do {t} encerrada: {y} x {no}. o admin vetou o kick","empate tecnico ({y} x {no}). {t} fica"};
 public static readonly string[] VoteAfter={"nao acredito, ele fica","democracia e isso ai","proxima eu kicko","kkkkk {t} sobreviveu","sistema roubado","ta bom, ta bom","{t} escapou dessa","fica, mas joga direito {t}"};
 public static readonly string[] VoteHuman={"o {h} votou 1 kkkk","{h} votou 2, ta defendendo o cara","{h} tambem quer kickar","ate o {h} votou","valeu pelo voto, {h}"};
 public static readonly string[] TicketLose={"bora time, ta feio o placar","vamos perder assim, pega bandeira","{n} tickets de diferenca, acorda time","se nao pegar a {f} a gente perde","ninguem defende nada nesse time"};
 public static readonly string[] TicketWin={"ta facil, segura o placar","mais um pouco e acabou","eles nao pegam nada kkk","so segurar as bandeiras","placar lindo, nao relaxa"};
 public static readonly string[] Winner={
  "gg, amassamos","ganhamos porra!","foi facil demais","chora, time perdedor","vitoria merecida","isso que e time","proxima rodada tem mais","gg ez",
  "obrigado pelas kills, otarios","nem suei","quem duvidou?","o time adversario e uma piada","gg time, jogaram muito","vencemos de virada, que delicia","fica de licao"};
 public static readonly string[] Loser={
  "time de merda, ninguem pegou bandeira","perdemos pra esses noobs, que vergonha","gg... na proxima eu carrego","que porra foi essa","nosso time so tinha sniper na colina",
  "caralho, que time ruim","perdemos por causa do tanque que ficou parado","nao quero falar sobre isso","revanche, agora","pqp, que derrota","gg, mereceram","proxima rodada a gente vira","culpa do lag"};
 public static readonly string[][] Casual={
  new[]{"alguem sabe se tem pizza depois da partida?","tem pizza nao, tem bala","entao vou morrer com fome"},
  new[]{"quem e o arrombado no helicoptero?","sou eu, por que?","porque voce bateu na arvore 3 vezes"},
  new[]{"eu juro que hoje eu vou ganhar uma","voce falou isso ontem","e ontem eu tava com lag"},
  new[]{"alguem joga uma municao pelo amor de deus","to sem tambem","time de pobre"},
  new[]{"esse mapa ta bugado?","o mapa nao, voce que ta","engracadinho"},
  new[]{"bora flanquear pela esquerda","esquerda de quem?","esquece, vou sozinho"},
  new[]{"quem ta de medico ai?","eu, mas to longe","entao nao serve pra nada"},
  new[]{"para de dar tiro em mim, eu sou do teu time","foi mal, achei que era inimigo","o nome ta em azul, porra"},
  new[]{"alguem viu meu tanque?","o que pegou fogo?","... era esse"},
  new[]{"quanto ta o placar?","ta feio","feio pra quem?","pra nos, claro"},
  new[]{"vou ali na bandeira e ja volto","volta inteiro","nao prometo nada"},
  new[]{"esse sniper nao erra uma","deve ser bot","todo mundo aqui e bot, genio"},
  new[]{"qual arma voces tao usando?","ak12, sempre","m16 aqui","faca, sou macho"},
  new[]{"alguem ai e de sp?","rj aqui","bh","sou do servidor mesmo kkk"},
  new[]{"quem joga bf3 ainda?","eu, saudade do caspian","esse aqui e melhor","mentira"},
  new[]{"esse jet ta voando ou caindo?","as duas coisas","kkkkk"},
  new[]{"bora fazer squad so de medico","e quem mata?","o inimigo de rir"},
  new[]{"to com 2 fps","compra um pc","compra pra mim"},
  new[]{"alguem joga ranked?","isso aqui nao tem ranked","entao to jogando o que?","sofrimento"},
  new[]{"quem pegou meu carro?","eu, mas capotei","de novo nao"},
  new[]{"fiz 30 kills a partida passada","print ou nao aconteceu","confia"},
  new[]{"o c4 funciona no tanque?","funciona se voce nao explodir junto","ja explodi"},
  new[]{"que horas acaba essa partida?","quando os tickets acabarem","nunca entao"},
  new[]{"alguem vai no b comigo?","vou","eu tambem","3 contra o mundo"},
  new[]{"bom dia servidor","bom dia","bom dia nada, ja morri 5 vezes"},
  new[]{"meu esquadrao sumiu","ta todo mundo morto","que esquadrao bom"},
  new[]{"alguem sabe onde fica o easter egg?","que easter egg?","esquece"},
  new[]{"eu sou o melhor sniper do servidor","quantas kills?","duas","lenda"},
  new[]{"esse servidor e brasileiro?","e sim","finalmente um sem ping alto"},
  new[]{"vou dormir depois dessa","fala isso toda partida","dessa vez e serio"},
 };
 // 0.5.33: assunto puxado por um bot e respondido por 2 ou 3 colegas (respostas sorteadas, sem ordem fixa).
 public sealed class Talk{public string[] Open,Replies,Close;public Talk(string[] o,string[] r,string[] c=null){Open=o;Replies=r;Close=c;}}
 public static readonly Talk[] Threads={
  new Talk(new[]{"nao tem time nao ai?","cade o time, porra?","to sozinho nessa bandeira?","alguem do time ta vivo?"},
   new[]{"to aqui, calma","tem nao, cada um por si","to nascendo, segura ai","to indo, 10 segundos","morri, foi mal","to no outro lado do mapa","time? que time?","to chegando pela {f}","segura que eu vou"},
   new[]{"demorou hein","agora sim","valeu, chega logo"}),
  new Talk(new[]{"quem ta de medico?","algum medico vivo?","preciso de revive aqui"},
   new[]{"eu, ja vou","to de assalto, vou ai","to longe, se vira","eu mas to sem desfibrilador","ninguem joga de medico nesse time","segura ai que eu revivo"},
   new[]{"vem logo","obrigado mano","esquece, ja dei respawn"}),
  new Talk(new[]{"alguem tem municao?","to sem bala","joga uma caixa ai pelo amor"},
   new[]{"to de suporte, vou jogar","acabou a minha tambem","pega do chao","to longe","joguei na {f}","usa a pistola kkk"},
   new[]{"valeu","peguei, obrigado","vou de faca entao"}),
  new Talk(new[]{"vamos atacar a {f} juntos?","bora todo mundo na {f}","quem vem comigo na {f}?"},
   new[]{"bora","to dentro","vou pela direita","eu cubro de longe","vai que eu vou atras","nao, defende a nossa primeiro","to indo de carro","3 segundos"},
   new[]{"bora entao","contando com voces","vai vai vai"}),
  new Talk(new[]{"quem e o sniper que ta matando todo mundo?","tem um sniper safado na colina","sniper la no alto, alguem resolve"},
   new[]{"vi ele, ta perto da {f}","deixa comigo","vou de fumaca","eu vi o brilho da luneta","ja morri pra ele duas vezes","alguem de tanque atira la"},
   new[]{"pegaram ele?","ainda ta la","resolvido, valeu"}),
  new Talk(new[]{"que time ruim, ninguem pega bandeira","o time ta todo de sniper","nosso time ta perdido"},
   new[]{"fala menos e joga mais","eu to pegando, voce que nao ve","verdade","to tentando, porra","calma, da pra virar","pior que e verdade","olha quem fala"},
   new[]{"entao bora","so reclamar nao adianta","vamo virar isso"}),
  new Talk(new[]{"alguem tira esse tanque daqui?","tanque inimigo na {f}","esse tanque ta destruindo a gente"},
   new[]{"vou de rpg","to de engenheiro, deixa comigo","joga c4 nele","ja dei dois tiros, falta pouco","alguem mira no traseiro dele","fugi dele kkk"},
   new[]{"explodiu!","ainda ta vivo","boa, menos um"}),
  new Talk(new[]{"cade o heli do nosso time?","quem pegou o helicoptero?","alguem pilota ai?"},
   new[]{"caiu","eu peguei, ja vou","bateu no predio","o piloto e ruim","pega voce entao","to esperando nascer"},
   new[]{"que piloto","ok","so faltava essa"}),
  new Talk(new[]{"ta tendo lag ai?","alguem mais travando?","o servidor ta pesado"},
   new[]{"aqui ta liso","um pouco","lag e desculpa","eu to com 300 de ping kkk","so na {f}","normal pra mim"},
   new[]{"entao e meu pc","pelo menos nao sou so eu","ta"}),
  new Talk(new[]{"quanto falta pra acabar?","quantos tickets eles tem?","ta perto de acabar?"},
   new[]{"falta muito","olha o placar, ta feio","uns 200","se pegar a {f} a gente vira","nao sei, to morrendo"},
   new[]{"bora entao","vixe","da tempo"}),
  new Talk(new[]{"alguem sabe jogar de recon?","quem bota um beacon?","bota spawn perto da {f}"},
   new[]{"eu boto","to de recon, ja vou","bota voce","coloquei atras do predio","o meu destruiram"},
   new[]{"valeu","ja vi","nasce no beacon galera"}),
  new Talk(new[]{"o {k} ta matando todo mundo","alguem segura o {k}?","o {k} nao morre"},
   new[]{"ja tentei","vamo todo mundo nele","ele ta na {f}","report nele","deixa que eu pego","ele ta de hack, certeza","e skill, aceita"},
   new[]{"bora pegar ele","marcado no mapa","um dia ele morre"}),
 };
 public static readonly string[] RageReply={"calma mano","eu tambem morri pra ele","joga junto que a gente pega ele","respira","acontece com todo mundo","kkkkk que fase",
  "relaxa, ele vai cair","vem pro meu lado","ta acontecendo comigo tambem","para de correr sozinho","foi feio mesmo","chora nao"};
 public static readonly string[] TauntReply={"kkkkkkk","humilhou","deixa ele chorar","essa doeu","kkkk ele nem viu","foi lindo","ta passando vergonha","print","que maldade kkk","respeita"};
}
public sealed class ChatBrain {
 // Team: 0 = geral (todos); >0 = só aquele time. Squad > 0 junto com Team = só aquele esquadrão.
 public sealed class Line{public DateTime At;public string Text;public string Who;public int Team;public int Squad;}
 public const string VoteName="VOTACAO",AiName="IA ANTICHEAT";
 readonly Random rnd;readonly Func<DateTime> now;readonly List<Line> queue=new List<Line>();
 readonly Dictionary<string,int> deaths=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase),kills=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase),
  total=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase),reports=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
 public readonly HashSet<string> Humans=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
 public readonly Dictionary<string,int> Team=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase),Squad=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
 public int Level=2;
 DateTime lastSay=DateTime.MinValue,nextCasual=DateTime.MinValue,nextCallout=DateTime.MinValue,nextReport=DateTime.MinValue,nextVote=DateTime.MinValue,nextAi=DateTime.MinValue,nextTicket=DateTime.MinValue,nextThread=DateTime.MinValue;
 readonly Dictionary<string,DateTime> lastBy=new Dictionary<string,DateTime>(StringComparer.OrdinalIgnoreCase);
 readonly Queue<string> recent=new Queue<string>();readonly HashSet<string> recentSet=new HashSet<string>();
 // Votação em andamento.
 sealed class Vote{public string Target;public int TargetTeam;public DateTime Ends;public int Yes,No;public HashSet<string> Voted=new HashSet<string>(StringComparer.OrdinalIgnoreCase);}
 Vote vote;
 public ChatBrain(int seed,Func<DateTime> clock){rnd=new Random(seed);now=clock;}
 public bool VoteOpen{get{return vote!=null;}}
 // Intervalo mínimo entre falas comuns (baixa 12-20 s, média 4-8 s, alta 2-4,5 s).
 TimeSpan Gap(){int a=Level>=3?2000:Level==2?4000:12000,b=Level>=3?4500:Level==2?8000:20000;return TimeSpan.FromMilliseconds(rnd.Next(a,b));}
 bool Chance(double p){return rnd.NextDouble()<p;}
 // Sorteia evitando as ~90 frases usadas por último (o chat antigo repetia demais).
 string Pick(string[] list){string s=list[rnd.Next(list.Length)];for(int i=0;i<12&&recentSet.Contains(s);i++)s=list[rnd.Next(list.Length)];
  if(recentSet.Add(s)){recent.Enqueue(s);while(recent.Count>90)recentSet.Remove(recent.Dequeue());}return s;}
 public bool IsBot(string name){return !String.IsNullOrEmpty(name)&&!Humans.Contains(name)&&name!=VoteName&&name!=AiName;}
 int TeamOf(string n){int t;return n!=null&&Team.TryGetValue(n,out t)?t:0;}
 int SquadOf(string n){int s;return n!=null&&Squad.TryGetValue(n,out s)?s:0;}
 bool HumanTeam(int t){return t>0&&Humans.Any(h=>TeamOf(h)==t);}
 static string Weapon(string w){if(String.IsNullOrEmpty(w))return "tiro";if(w.StartsWith("U_"))w=w.Substring(2);return w.Replace('_',' ');}
 public static string Fill(string text,string k,string v,string w,int n,string h){return text.Replace("{k}",k??"").Replace("{v}",v??"").Replace("{w}",Weapon(w)).Replace("{n}",n.ToString()).Replace("{h}",h??"");}
 static string Flag(Random r){return ((char)('A'+r.Next(5))).ToString();}
 public static string Format(string who,string text){string s=who+": "+text; /* sem colchetes: o jogo já mostra [ADMINISTRADOR] antes (19/11) */return s.Length>127?s.Substring(0,127):s;}
 bool Ready(){return now()>=lastSay;}
 // channel: 0 = geral; 1 = escolhe sozinho (time do jogador fala às vezes no time/esquadrão); 2 = sempre no time quando o jogador está nele.
 bool Say(string who,string text,int delayMs,int channel=1,bool force=false){
  if(String.IsNullOrEmpty(who)||Level<=0)return false;if(!force&&queue.Count>=12)return false;DateTime at=now().AddMilliseconds(delayMs);
  DateTime prev;if(!force&&lastBy.TryGetValue(who,out prev)&&at<prev.AddSeconds(5))return false;lastBy[who]=at;
  text=text.Replace("{f}",Flag(rnd));
  int team=0,squad=0,t=TeamOf(who);
  if(channel>0&&IsBot(who)&&HumanTeam(t)&&(channel==2||Chance(0.5))){team=t;int sq=SquadOf(who);if(sq>0&&Humans.Any(h=>TeamOf(h)==t&&SquadOf(h)==sq)&&Chance(0.5))squad=sq;}
  // 0.5.33: a cor por time não muda pelo RCON, então o time do jogador leva [TIME] antes do nome.
  queue.Add(new Line{At=at,Text=Format((IsBot(who)&&HumanTeam(t)?"[TIME] ":"")+who,text),Who=who,Team=team,Squad=squad});return true;
 }
 void Commit(){lastSay=now()+Gap();}
 // Falas prontas para enviar agora.
 public List<Line> DueLines(){var t=now();var due=queue.Where(l=>l.At<=t).OrderBy(l=>l.At).ToList();foreach(var l in due)queue.Remove(l);return due;}
 public List<string> Due(){return DueLines().Select(l=>l.Text).ToList();}
 public int Pending{get{return queue.Count;}}
 string OtherBot(params string[] not){var pool=Team.Keys.Where(n=>IsBot(n)&&!not.Contains(n,StringComparer.OrdinalIgnoreCase)).ToList();return pool.Count==0?null:pool[rnd.Next(pool.Count)];}
 string BotOf(int team,params string[] not){var pool=Team.Where(p=>p.Value==team&&IsBot(p.Key)&&!not.Contains(p.Key,StringComparer.OrdinalIgnoreCase)).Select(p=>p.Key).ToList();return pool.Count==0?null:pool[rnd.Next(pool.Count)];}
 List<string> BotsOf(int team,int count,params string[] not){return Team.Where(p=>(team==0||p.Value==team)&&IsBot(p.Key)&&!not.Contains(p.Key,StringComparer.OrdinalIgnoreCase)).Select(p=>p.Key).OrderBy(x=>rnd.Next()).Take(count).ToList();}
 int Enemy(int team){return team==1?2:team==2?1:0;}
 public void OnKill(string killer,string victim,string weapon,bool headshot){
  if(String.IsNullOrEmpty(victim))return;
  int d;deaths.TryGetValue(victim,out d);deaths[victim]=d+1;kills[victim]=0;
  int kc=0,tk=0;bool self=String.IsNullOrEmpty(killer)||String.Equals(killer,victim,StringComparison.OrdinalIgnoreCase);
  if(!self){kills.TryGetValue(killer,out kc);kills[killer]=++kc;deaths[killer]=0;total.TryGetValue(killer,out tk);total[killer]=++tk;}
  if(Level<=0)return;
  double scale=Level>=3?1.0:Level==2?0.75:0.45;
  string kk=killer,vv=victim;int vd=d+1;
  if(self){if(IsBot(victim)&&Ready()&&Chance(0.5*scale)){Say(victim,Pick(ChatPhrases.Suicide),rnd.Next(800,1800));Commit();}return;}
  bool kb=IsBot(killer),vb=IsBot(victim);
  // Denúncia: quem domina a partida (bot ou jogador) leva report, mesmo com o chat ocupado.
  if(vb&&now()>=nextReport&&(kc>=4||(tk>=8&&tk%4==0))&&Chance((kb?0.45:0.8)*scale)){Report(killer,victim,tk);return;}
  if(!Ready())return;
  if(vb&&!kb&&Chance(0.6*scale)){Say(victim,Fill(Pick(ChatPhrases.VsHuman),kk,vv,weapon,vd,null),rnd.Next(900,2200),0);Commit();MaybePraise(killer,victim);return;}
  if(vb&&!kb){MaybePraise(killer,victim);}
  if(kb&&!vb&&Chance(0.55*scale)){Say(killer,Fill(Pick(ChatPhrases.HumanTaunt),kk,vv,weapon,kc,null),rnd.Next(700,1600),0);Commit();return;}
  if(vb&&vd>=6&&vote==null&&now()>=nextVote&&Chance(0.3*scale)){StartVote(victim,TeamOf(victim),true,vd);return;}
  if(vb&&vd>=3&&Chance(0.65*scale)){Say(victim,Fill(Pick(ChatPhrases.Rage),kk,vv,weapon,vd,null),rnd.Next(900,2200));Commit();MaybeReply(victim,killer,ChatPhrases.RageReply);return;}
  if(kb&&kc>=5&&kc%5==0&&Chance(0.75*scale)){Say(killer,Fill(Pick(ChatPhrases.Streak),kk,vv,weapon,kc,null),rnd.Next(700,1500));Commit();MaybeReply(killer,victim,ChatPhrases.TauntReply);return;}
  if(headshot&&Chance(0.45*scale)){if(kb&&Chance(0.6))Say(killer,Fill(Pick(ChatPhrases.Headshot),kk,vv,weapon,kc,null),rnd.Next(700,1500));else if(vb)Say(victim,Fill(Pick(ChatPhrases.HeadshotVictim),kk,vv,weapon,vd,null),rnd.Next(900,2000));Commit();MaybeReply(vb?victim:killer,null);return;}
  if(kb&&Chance(0.22*scale)){Say(killer,Fill(Pick(ChatPhrases.Taunt),kk,vv,weapon,kc,null),rnd.Next(700,1600));Commit();MaybeReply(killer,victim,ChatPhrases.TauntReply);return;}
  if(vb&&Chance(0.16*scale)){Say(victim,Fill(Pick(ChatPhrases.Rage),kk,vv,weapon,vd,null),rnd.Next(900,2000));Commit();MaybeReply(victim,killer,ChatPhrases.RageReply);}
 }
 // 0.5.33: até 3 respostas seguidas, do mesmo time de quem falou, com frase que combina com o assunto.
 void MaybeReply(string first,string mention,string[] pool=null){if(Level<2||!Chance(Level>=3?0.65:0.4))return;
  int count=Level>=3?rnd.Next(1,4):rnd.Next(1,3);var used=new List<string>{first};if(mention!=null)used.Add(mention);int at=rnd.Next(2200,3600);
  for(int i=0;i<count;i++){string who=BotOf(TeamOf(first),used.ToArray())??OtherBot(used.ToArray());if(who==null)break;used.Add(who);
   Say(who,Pick(i==0&&pool!=null?pool:Chance(0.5)&&pool!=null?pool:ChatPhrases.Reply),at,1,true);at+=rnd.Next(1800,3200);}}
 // Assunto puxado por um bot do time do jogador (ou qualquer um) e respondido por 2-3 colegas.
 void StartThread(){
  var th=ChatPhrases.Threads[rnd.Next(ChatPhrases.Threads.Length)];for(int g=0;g<8&&recentSet.Contains(th.Open[0]);g++)th=ChatPhrases.Threads[rnd.Next(ChatPhrases.Threads.Length)];
  if(recentSet.Add(th.Open[0])){recent.Enqueue(th.Open[0]);while(recent.Count>90)recentSet.Remove(recent.Dequeue());}
  var humanTeams=Humans.Select(TeamOf).Where(x=>x>0).Distinct().ToList();int team=humanTeams.Count>0&&Chance(0.7)?humanTeams[rnd.Next(humanTeams.Count)]:(Chance(0.5)?1:2);
  string opener=BotOf(team);if(opener==null)return;
  string target=kills.Where(p=>p.Value>=3&&TeamOf(p.Key)!=team).OrderByDescending(p=>p.Value).Select(p=>p.Key).FirstOrDefault();
  if(th.Open[0].Contains("{k}")&&target==null)return;
  Func<string,string> fill=s=>s.Replace("{k}",target??"");
  lastBy.Remove(opener);Say(opener,fill(Pick(th.Open)),rnd.Next(300,800),2,true);
  int n=Level>=3?rnd.Next(2,4):2,at=rnd.Next(2500,3800);var used=new List<string>{opener};
  for(int i=0;i<n;i++){string who=BotOf(team,used.ToArray());if(who==null)break;used.Add(who);Say(who,fill(Pick(th.Replies)),at,2,true);at+=rnd.Next(1700,3200);}
  if(th.Close!=null&&Chance(0.6)){lastBy.Remove(opener);Say(opener,Pick(th.Close),at+rnd.Next(800,1600),2,true);at+=1600;}
  lastSay=now().AddMilliseconds(at+1500);
 }
 // Colega de time do jogador comemora a kill dele (aparece no canal do time).
 void MaybePraise(string human,string victim){if(Level<2||!Chance(Level>=3?0.35:0.2))return;string mate=BotOf(TeamOf(human),victim);if(mate!=null)Say(mate,Fill(Pick(ChatPhrases.HumanKillPraise),human,victim,null,0,human),rnd.Next(1800,3200),2);}
 void Report(string target,string victim,int tk){
  int at=rnd.Next(900,1900);Say(victim,Fill(Pick(ChatPhrases.Report),target,victim,null,tk,null),at,0,true);
  int n;reports.TryGetValue(target,out n);n++;
  int echoes=Level>=3?rnd.Next(1,4):Level==2?rnd.Next(0,3):0;
  foreach(var who in BotsOf(TeamOf(victim),echoes,victim,target)){at+=rnd.Next(1500,3200);if(Say(who,Fill(Pick(ChatPhrases.ReportEcho),target,victim,null,tk,null),at,0,true))n++;}
  if(Level>=2&&Chance(0.4)){string mate=BotOf(TeamOf(target),target);if(mate!=null){at+=rnd.Next(1500,3000);Say(mate,Fill(Pick(ChatPhrases.ReportDefend),target,victim,null,tk,null),at,0,true);}}
  reports[target]=n;nextReport=now().AddSeconds(Level>=3?25:Level==2?45:90);lastSay=now().AddMilliseconds(at+1500);
  if(n>=3&&vote==null&&now()>=nextVote&&Chance(0.55)){string starter=BotOf(TeamOf(victim),target)??victim;StartVoteLater(target,TeamOf(target),starter,at+rnd.Next(2500,4500));}
  else if(n>=2&&now()>=nextAi&&Chance(0.6))AiCheck(target,n,at+rnd.Next(2500,4500));
 }
 void AiCheck(string target,int count,int at){
  Say(AiName,Pick(ChatPhrases.AiStart).Replace("{t}",target).Replace("{n}",count.ToString()),at,0,true);at+=rnd.Next(6000,9000);
  Say(AiName,Pick(ChatPhrases.AiClear).Replace("{t}",target),at,0,true);
  foreach(var who in BotsOf(Enemy(TeamOf(target)),Level>=3?2:1,target)){at+=rnd.Next(1500,3000);Say(who,Pick(ChatPhrases.AiReact),at,0,true);}
  reports[target]=0;nextAi=now().AddSeconds(Level>=3?100:Level==2?160:300);lastSay=now().AddMilliseconds(at+1500);
 }
 void StartVoteLater(string target,int targetTeam,string starter,int at){StartVote(target,targetTeam,false,0,starter,at);}
 // own = o próprio time quer kickar quem só morre. Ninguém é expulso de verdade: o resultado nunca passa.
 void StartVote(string target,int targetTeam,bool own,int deathsNow,string starter=null,int at=0){
  if(Level<=0||vote!=null)return;
  if(starter==null)starter=own?BotOf(targetTeam,target):BotOf(Enemy(targetTeam),target);if(starter==null)return;
  if(at==0)at=rnd.Next(1000,2000);
  string open=Pick(own?ChatPhrases.VoteStartOwn:ChatPhrases.VoteStart).Replace("{t}",target).Replace("{n}",deathsNow.ToString());
  Say(starter,open,at,0,true);
  var v=new Vote{Target=target,TargetTeam=targetTeam,Ends=now().AddMilliseconds(at+rnd.Next(17000,22000))};v.Voted.Add(starter);v.Yes=1;
  int voters=Level>=3?rnd.Next(6,10):Level==2?rnd.Next(4,7):rnd.Next(2,4);int t=at;
  foreach(var who in BotsOf(0,voters,target,starter)){
   bool yes=own?(TeamOf(who)==targetTeam?Chance(0.7):Chance(0.3)):(TeamOf(who)==targetTeam?Chance(0.2):Chance(0.8));
   t+=rnd.Next(900,2200);if(Say(who,Pick(yes?ChatPhrases.VoteYes:ChatPhrases.VoteNo).Replace("{t}",target),t,0,true)){v.Voted.Add(who);if(yes)v.Yes++;else v.No++;}
  }
  vote=v;nextVote=now().AddSeconds(Level>=3?150:Level==2?240:420);lastSay=v.Ends.AddSeconds(4);
 }
 void CloseVote(){
  var v=vote;vote=null;if(v==null)return;
  // Nunca passa: pelo menos 40% de "2" ou o admin veta (a Central não expulsa ninguém).
  int yes=v.Yes,no=v.No;if(no*3<yes*2&&Chance(0.5))no=Math.Max(no,(int)Math.Ceiling(yes*0.7));
  Say(VoteName,Pick(ChatPhrases.VoteResult).Replace("{t}",v.Target).Replace("{y}",yes.ToString()).Replace("{no}",no.ToString()),300,0,true);
  int at=300;foreach(var who in BotsOf(0,Level>=3?2:1,v.Target)){at+=rnd.Next(1500,3000);Say(who,Pick(ChatPhrases.VoteAfter).Replace("{t}",v.Target),at,0,true);}
  lastSay=now().AddMilliseconds(at+2000);
 }
 public void OnChat(string who,string text,string subset){
  if(Level<=0||String.IsNullOrEmpty(who)||who=="Server"||IsBot(who)||who==VoteName||who==AiName)return;
  if(text==null||text.StartsWith("!")||text.StartsWith("ID_CHAT"))return;
  string t=text.Trim().ToLowerInvariant();
  // Voto do jogador durante a votação.
  if(vote!=null&&(t=="1"||t=="2"||t=="um"||t=="dois")&&vote.Voted.Add(who)){if(t=="1"||t=="um")vote.Yes++;else vote.No++;
   if(Chance(0.7)){string b=OtherBot(vote.Target);if(b!=null)Say(b,Fill(Pick(ChatPhrases.VoteHuman),null,null,null,0,who),rnd.Next(1200,2500),1,true);}return;}
  if(t=="1"||t=="2")return;
  if(!Chance(Level>=3?0.85:Level==2?0.65:0.4))return;
  string bot=OtherBot();if(bot==null)return;string[] pool;
  if(t.Contains("gg"))pool=ChatPhrases.HumanGg;else if(t.Contains("hack")||t.Contains("cheat")||t.Contains("report"))pool=ChatPhrases.HumanHack;else if(t.Contains("bot"))pool=ChatPhrases.HumanBot;
  else if(new[]{"porra","caralho","lixo","noob","merda","fdp","puta","vtnc","arrombad","otario","burro"}.Any(t.Contains))pool=ChatPhrases.HumanInsult;
  else if(t.Contains("kkk")||t.Contains("haha")||t.Contains("rsrs"))pool=ChatPhrases.HumanLaugh;else pool=ChatPhrases.HumanChatReply;
  Say(bot,Fill(Pick(pool),null,null,null,0,who),rnd.Next(1500,3200),1,true);
  if(Level>=3&&Chance(0.3)){string b2=OtherBot(bot);if(b2!=null)Say(b2,Pick(ChatPhrases.Reply),rnd.Next(3800,5500));}
 }
 // Placar (tickets) lido a cada ~30 s: o time que perde cobra, o que ganha provoca.
 public void OnScores(float a,float b){
  if(Level<2||now()<nextTicket||!Ready())return;float hi=Math.Max(a,b),lo=Math.Min(a,b);if(hi<=0||hi-lo<Math.Max(60f,hi*0.2f))return;
  int lose=a<b?1:2;bool loser=Chance(0.6);string who=BotOf(loser?lose:Enemy(lose));if(who==null)return;
  Say(who,Pick(loser?ChatPhrases.TicketLose:ChatPhrases.TicketWin).Replace("{n}",((int)(hi-lo)).ToString()),rnd.Next(500,1500),2);Commit();
  nextTicket=now().AddSeconds(Level>=3?70:120);
 }
 public void OnRoundOver(int winner){
  vote=null;
  if(Level<=0)return;int count=Level>=3?14:Level==2?8:3;var bots=Team.Where(p=>IsBot(p.Key)).ToList();if(bots.Count==0)return;
  int delay=1200;
  for(int i=0;i<count;i++){bool win=i%2==0;var side=bots.Where(p=>(p.Value==winner)==win).ToList();if(side.Count==0)continue;var who=side[rnd.Next(side.Count)].Key;
   lastBy.Remove(who);Say(who,Pick(win?ChatPhrases.Winner:ChatPhrases.Loser),delay,0,true);delay+=rnd.Next(700,1300);}
  lastSay=now().AddMilliseconds(delay+20000);deaths.Clear();kills.Clear();total.Clear();reports.Clear();
 }
 // Conversa à toa, chamadas táticas e recado para o jogador, de tempos em tempos.
 public void Tick(){
  var t=now();
  if(vote!=null&&t>=vote.Ends)CloseVote();
  if(Level<1)return;
  if(nextCasual==DateTime.MinValue){nextCasual=t.AddSeconds(rnd.Next(25,50));nextCallout=t.AddSeconds(rnd.Next(10,20));return;}
  if(Level>=2&&t>=nextCallout&&Ready()){
   nextCallout=t.AddSeconds(Level>=3?rnd.Next(12,22):rnd.Next(25,40));
   var humanTeams=Humans.Select(TeamOf).Where(x=>x>0).Distinct().ToList();int team=humanTeams.Count>0&&Chance(0.75)?humanTeams[rnd.Next(humanTeams.Count)]:0;
   string who=team>0?BotOf(team):OtherBot();if(who!=null){
    string h=Humans.Where(x=>TeamOf(x)==TeamOf(who)).OrderBy(x=>rnd.Next()).FirstOrDefault();
    if(h!=null&&Chance(0.25))Say(who,Fill(Pick(ChatPhrases.ToHuman),null,null,null,0,h),rnd.Next(300,900),2);
    else Say(who,Pick(Chance(0.8)?ChatPhrases.Callout:ChatPhrases.Generic),rnd.Next(300,900),2);
    if(Level>=3&&Chance(0.3)){string mate=BotOf(TeamOf(who),who);if(mate!=null)Say(mate,Pick(ChatPhrases.Reply),rnd.Next(2500,4000),2);}
    Commit();}
   return;
  }
  if(nextThread==DateTime.MinValue)nextThread=t.AddSeconds(rnd.Next(15,30));
  if(Level>=2&&t>=nextThread&&Ready()){nextThread=t.AddSeconds(Level>=3?rnd.Next(25,45):rnd.Next(50,80));StartThread();return;}
  if(Level<2||t<nextCasual||!Ready())return;nextCasual=t.AddSeconds(Level>=3?rnd.Next(35,60):rnd.Next(70,110));
  var talk=ChatPhrases.Casual[rnd.Next(ChatPhrases.Casual.Length)];for(int g=0;g<8&&recentSet.Contains(talk[0]);g++)talk=ChatPhrases.Casual[rnd.Next(ChatPhrases.Casual.Length)];
  if(recentSet.Add(talk[0])){recent.Enqueue(talk[0]);while(recent.Count>90)recentSet.Remove(recent.Dequeue());}
  var people=new List<string>();
  for(int i=0;i<talk.Length;i++){string who=i>=2&&people.Count>=2&&talk.Length==3?people[0]:OtherBot(people.ToArray());if(who==null)return;people.Add(who);}
  int delay=500;for(int i=0;i<talk.Length;i++){lastBy.Remove(people[i]);Say(people[i],talk[i],delay,1,true);delay+=rnd.Next(2500,4000);}Commit();
 }
 public void SetPlayers(IEnumerable<KeyValuePair<string,int>> team,IEnumerable<string> humans,IEnumerable<KeyValuePair<string,int>> squads=null){
  Team.Clear();foreach(var p in team)Team[p.Key]=p.Value;Humans.Clear();foreach(var h in humans)Humans.Add(h);Squad.Clear();if(squads!=null)foreach(var s in squads)Squad[s.Key]=s.Value;}
}
// Liga/desliga e nível (arquivo em %LOCALAPPDATA%\BF4BotLauncher\chat-portugues.txt: "pt|nível").
public static class ChatPt {
 static string File_{get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BF4BotLauncher","chat-portugues.txt");}}
 public static bool Enabled{get{return File.Exists(File_);}}
 public static int Level{get{try{var p=File.ReadAllText(File_).Trim().Split('|');int l;return p.Length>1&&Int32.TryParse(p[1],out l)&&l>=0&&l<=3?l:2;}catch{return 0;}}}
 public static void Set(bool on,int level){if(on){Directory.CreateDirectory(Path.GetDirectoryName(File_));File.WriteAllText(File_,"pt|"+Math.Max(0,Math.Min(3,level)));}else if(File.Exists(File_))File.Delete(File_);}
 public static volatile string LastStatus="Chat em português desligado.";
 static Thread worker;
 // Roda enquanto a Central estiver aberta; liga sozinho quando o servidor (local ou PC servidor) responde.
 public static void Start(Func<Dictionary<string,string>> config){if(worker!=null)return;worker=new Thread(()=>Loop(config)){IsBackground=true,Name="ChatPt"};worker.Start();}
 static void Loop(Func<Dictionary<string,string>> config){
  var brain=new ChatBrain(Environment.TickCount,()=>DateTime.UtcNow);
  while(true){
   try{
    if(!Enabled||Level<=0){LastStatus="Chat em português desligado.";Thread.Sleep(5000);continue;}
    var c=config();string host=Settings.Get(c,"RconHost","127.0.0.1"),pw=Settings.Get(c,"RconPassword","");int port;if(!Int32.TryParse(Settings.Get(c,"RconPort","25210"),out port))port=25210;
    Session(brain,host,port,pw);
   }catch(Exception ex){LastStatus="Chat em português aguardando o servidor: "+ex.Message;}
   Thread.Sleep(8000);
  }
 }
 static void Session(ChatBrain brain,string host,int port,string pw){
  using(var events=new TcpClient())using(var say=new Rcon()){
   var ar=events.BeginConnect(host,port,null,null);if(!ar.AsyncWaitHandle.WaitOne(5000))throw new TimeoutException("sem resposta");events.EndConnect(ar);events.NoDelay=true;
   var s=events.GetStream();s.ReadTimeout=5000;s.WriteTimeout=5000;uint seq=1;
   var salt=Ask(s,seq++,"login.hashed");if(salt.Length<2||salt[1].Length!=32)throw new InvalidDataException("login");
   byte[] bytes=Enumerable.Range(0,16).Select(i=>Convert.ToByte(salt[1].Substring(i*2,2),16)).Concat(Encoding.UTF8.GetBytes(pw)).ToArray();string hash;using(var md5=MD5.Create())hash=BitConverter.ToString(md5.ComputeHash(bytes)).Replace("-","");
   var ok=Ask(s,seq++,"login.hashed",hash);if(ok.Length==0||ok[0]!="OK")throw new InvalidOperationException("senha RCON recusada");
   Ask(s,seq++,"admin.eventsEnabled","true");
   say.Connect(host,port,pw);Players(brain,say);DateTime nextPlayers=DateTime.UtcNow.AddSeconds(20),nextScores=DateTime.UtcNow.AddSeconds(30);
   LastStatus="Chat em português ativo ("+host+").";
   while(Enabled&&Level>0){
    brain.Level=Level;
    if(events.Client.Poll(500000,SelectMode.SelectRead)){
     uint flags;var words=Read(s,out flags);if(words==null)throw new IOException("conexão encerrada");
     if((flags&0x40000000)==0){var reply=Rcon.Encode((flags&0x3fffffff)|0x40000000,new[]{"OK"});s.Write(reply,0,reply.Length);Handle(brain,say,words);}
    }
    if(DateTime.UtcNow>=nextPlayers){Players(brain,say);nextPlayers=DateTime.UtcNow.AddSeconds(20);}
    if(DateTime.UtcNow>=nextScores){Scores(brain,say);nextScores=DateTime.UtcNow.AddSeconds(30);}
    brain.Tick();
    foreach(var line in brain.DueLines())Send(say,line);
   }
  }
 }
 // Time do jogador: canal do time ou do esquadrão (cores próprias no jogo). Se o servidor recusar, cai no geral.
 static void Send(Rcon say,ChatBrain.Line line){
  try{
   if(line.Team>0&&line.Squad>0){say.Command("admin.say",line.Text,"squad",line.Team.ToString(),line.Squad.ToString());return;}
   if(line.Team>0){say.Command("admin.say",line.Text,"team",line.Team.ToString());return;}
  }catch(InvalidOperationException){}
  say.Command("admin.say",line.Text,"all");
 }
 static void Scores(ChatBrain brain,Rcon say){
  try{var w=say.Command("serverInfo");int n;if(w.Length<10||!Int32.TryParse(w[8],out n)||n<2||w.Length<9+n)return;float a,b;
   if(Single.TryParse(w[9],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out a)&&Single.TryParse(w[10],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out b))brain.OnScores(a,b);}
  catch(InvalidOperationException){}
 }
 static void Handle(ChatBrain brain,Rcon say,string[] w){
  if(w.Length==0)return;
  switch(w[0]){
   case "player.onKill":if(w.Length>=5)brain.OnKill(w[1],w[2],w[3],w[4]=="true");break;
   case "player.onChat":if(w.Length>=3)brain.OnChat(w[1],w[2],w.Length>3?w[3]:"all");break;
   case "server.onRoundOver":{int team;Players(brain,say);if(w.Length>=2&&Int32.TryParse(w[1],out team))brain.OnRoundOver(team);break;}
   case "player.onJoin":case "player.onLeave":case "player.onTeamChange":case "player.onSquadChange":Players(brain,say);break;
  }
 }
 // Bot = sem GUID e ping 65535 (como o mod mostra no placar); o resto é jogador de verdade.
 static void Players(ChatBrain brain,Rcon say){
  var p=say.Command("listPlayers","all");int n;if(p.Length<3||!Int32.TryParse(p[1],out n)||n<1)return;var cols=p.Skip(2).Take(n).ToList();int count;if(!Int32.TryParse(p[2+n],out count))return;
  int iName=cols.IndexOf("name"),iGuid=cols.IndexOf("guid"),iTeam=cols.IndexOf("teamId"),iSquad=cols.IndexOf("squadId"),iPing=cols.IndexOf("ping");if(iName<0)return;
  var team=new List<KeyValuePair<string,int>>();var squads=new List<KeyValuePair<string,int>>();var humans=new List<string>();
  for(int r=0;r<count;r++){int b=3+n+r*n;if(b+n>p.Length)break;string name=p[b+iName];int t=0,sq=0;if(iTeam>=0)Int32.TryParse(p[b+iTeam],out t);if(iSquad>=0)Int32.TryParse(p[b+iSquad],out sq);
   team.Add(new KeyValuePair<string,int>(name,t));squads.Add(new KeyValuePair<string,int>(name,sq));
   bool bot=(iGuid<0||p[b+iGuid]=="")&&(iPing<0||p[b+iPing]=="65535");if(!bot)humans.Add(name);}
  brain.SetPlayers(team,humans,squads);
 }
 static string[] Ask(NetworkStream s,uint id,params string[] words){var b=Rcon.Encode(id,words);s.Write(b,0,b.Length);for(int i=0;i<64;i++){uint f;var r=Read(s,out f);if(r==null)throw new IOException("conexão encerrada");if((f&0x40000000)!=0&&(f&0x3fffffff)==id)return r;}throw new IOException("sem resposta ao comando");}
 static string[] Read(NetworkStream s,out uint flags){flags=0;var h=Exact(s,12);if(h==null)return null;uint size=BitConverter.ToUInt32(h,4);if(size<12||size>2*1024*1024)throw new InvalidDataException("pacote RCON fora do limite");var body=Exact(s,(int)size-12);if(body==null)return null;flags=BitConverter.ToUInt32(h,0);return Rcon.Decode(h.Concat(body).ToArray());}
 static byte[] Exact(NetworkStream s,int count){var b=new byte[count];int o=0;while(o<count){int n=s.Read(b,o,count-o);if(n<=0)return null;o+=n;}return b;}
}
}
