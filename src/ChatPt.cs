using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
namespace BF4Bots {
// Chat dos bots em português, feito pela Central (19/11/2026).
// As falas do mod ficam embutidas e criptografadas na dinput8.dll (só inglês/russo/pinyin), então a Central
// desliga o chat do mod e escreve as falas pelo RCON (admin.say), reagindo aos eventos do servidor
// (player.onKill, player.onChat, server.onRoundOver). A mensagem sai como aviso do servidor com o nome do bot no texto.
// Sem acentos de propósito: o chat do BF4 nem sempre mostra acentos e o pessoal digita assim mesmo.
public static class ChatPhrases {
 // {k} = quem matou, {v} = quem morreu, {w} = arma, {n} = sequência, {h} = jogador de verdade citado
 public static readonly string[] Rage={
  "puta que pariu, de novo?","caralho, nao consigo nem nascer direito","porra {k}, me deixa em paz","{n} mortes seguidas, que desgraca","vou desinstalar essa merda",
  "{k} filho da puta, ta me cacando?","pqp, nasci e ja morri","alguem mata esse {k} pelo amor de deus","cacete, que fase","to tomando mais tiro que alvo de treino",
  "nao e possivel, de novo esse arrombado","{k} vai tomar no cu","essa porra de spawn ta amaldicoada","desisto, vou jogar paciencia","morri {n} vezes sem dar um tiro, que lixo",
  "ta de sacanagem comigo, {k}","eu juro que ele ta de hack","meu deus, que dia de merda","de novo nao, porra","quem me colocou nesse time de bosta?",
  "sera que da pra parar de me matar, {k}?","vtnc {k}","to virando estatistica aqui","caralho, me pegou ate dormindo","vou ali chorar e ja volto"};
 public static readonly string[] Taunt={
  "senta, {v}","chora {v} kkkkk","vai pro lobby, {v}","foi mal {v}, nao foi nada pessoal... mentira, foi sim","de nada pelo respawn, {v}",
  "{v}, tenta de novo que ta facil","kkkkkkk tomou {v}","bota no replay essa, {v}","mais um pra conta","{v} veio de presente",
  "achou que ia onde, {v}?","volta pro tutorial, {v}","{v} caiu igual pato","pegou o {w} na cara, {v}","pode reclamar com o bispo, {v}",
  "toma, {v}, e leva pra casa","{v} nem viu de onde veio kkk","ez","essa foi bonita, admite {v}","{v} correndo em linha reta, ai fica facil"};
 public static readonly string[] HumanTaunt={
  "e ai {v}, so isso?","{v} achou que bot nao mira kkkkk","o humano caiu, chama o suporte","{v}, ce tava de olho fechado?","toma {v}, bot tambem joga",
  "{v} vai culpar o lag agora","kkkkkk {v} tomou de bot","{v}, quer que eu deixe mais facil?","manda o proximo, {v} ja era","{v} rage quit em 3, 2, 1..."};
 public static readonly string[] VsHuman={
  "{k} ta jogando muito, porra","caralho {k}, pega leve","{k} ta de hack, certeza","que tiro foi esse, {k}?","{k} filho da puta, que mira",
  "alguem segura esse {k}","{k} vai tomar no cu, serio","respeito, {k}... mas vai se fuder","{k} ta destruindo, chama reforco","mano, o {k} nao erra"};
 public static readonly string[] Headshot={
  "na cabeca, porra!","headshot, {v} ficou sem tampa","bem no meio da testa","{v} perdeu o capacete e a dignidade","que tiro lindo, nem eu acreditei",
  "boom, headshot","{v} levou na moleira","essa foi na tampa, {v}","de longe e na cabeca, respeita","mira laser hoje"};
 public static readonly string[] HeadshotVictim={"na cabeca nao, porra","de onde veio isso caralho","{k} me deu um headshot do outro lado do mapa, que merda","tiro na testa, que vergonha"};
 public static readonly string[] Streak={
  "{n} seguidas, ninguem me para","to on fire, {n} kills","alguem me para? ninguem? beleza","{n} sem morrer, chama o medico pro outro time","que sequencia gostosa, {n}",
  "to sozinho carregando esse time","tragam mais, {n} ja foram","modo deus ativado, {n} kills"};
 public static readonly string[] Suicide={"quem colocou essa granada ai? ah, fui eu","me matei sozinho, que orgulho","cai feito um saco de batata","morri pra gravidade, que vergonha"};
 public static readonly string[] Generic={
  "bora time, segura a bandeira","alguem tem municao ai?","medico! cade a porra do medico?","pega a bandeira C, caralho","sniper na colina, cuidado",
  "esse mapa e uma merda pra correr","quem ta no tanque? usa direito pelo amor de deus","to sem kit, alguem joga uma caixa","segura ai que eu to chegando","cade o esquadrao? cada um pra um lado"};
 public static readonly string[] Reply={
  "kkkkkkk","verdade","para de chorar","nem fala","concordo, que merda","foco no jogo, porra","relaxa que a gente vira","kkkk tomou","f","calma ai, mano"};
 public static readonly string[] HumanChatReply={
  "fala {h}","{h} ta falando muito pra quem ta morrendo","kkkkk {h}","respeita os bots, {h}","{h}, cala a boca e joga","bora {h}, ajuda o time",
  "nao enche {h}","{h} ce ta de que lado?","olha o {h} se achando","pode crer, {h}"};
 public static readonly string[] HumanGg={"gg {h}","gg wp","gg, partida boa","gg, revanche?"};
 public static readonly string[] HumanBot={"bot e a vo, {h}","bot que te mata, ne {h}?","eu sou bot mas tenho sentimentos, {h}","bot sim, ruim nao"};
 public static readonly string[] HumanInsult={"vai tomar no cu voce, {h}","olha a boca, {h}... arrombado","fala isso na minha cara, {h}","{h} ta nervosinho kkkkk"};
 public static readonly string[] HumanLaugh={"kkkkkkkkkk","rindo de que, {h}?","kkkk o {h} ta feliz","ri agora, {h}, depois chora"};
 public static readonly string[] Winner={
  "gg, amassamos","ganhamos porra!","foi facil demais","chora, time perdedor","vitoria merecida","isso que e time","proxima rodada tem mais","gg ez",
  "obrigado pelas kills, otarios","nem suei","quem duvidou?","o time adversario e uma piada"};
 public static readonly string[] Loser={
  "time de merda, ninguem pegou bandeira","perdemos pra esses noobs, que vergonha","gg... na proxima eu carrego","que porra foi essa","nosso time so tinha sniper na colina",
  "caralho, que time ruim","perdemos por causa do tanque que ficou parado","nao quero falar sobre isso","revanche, agora","pqp, que derrota"};
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
 };
}
public sealed class ChatBrain {
 public sealed class Line{public DateTime At;public string Text;}
 readonly Random rnd;readonly Func<DateTime> now;readonly List<Line> queue=new List<Line>();
 readonly Dictionary<string,int> deaths=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase),kills=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
 public readonly HashSet<string> Humans=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
 public readonly Dictionary<string,int> Team=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
 public int Level=2;
 DateTime lastSay=DateTime.MinValue,nextCasual=DateTime.MinValue;readonly Dictionary<string,DateTime> lastBy=new Dictionary<string,DateTime>(StringComparer.OrdinalIgnoreCase);
 public ChatBrain(int seed,Func<DateTime> clock){rnd=new Random(seed);now=clock;}
 // Intervalo mínimo entre falas (como no mod: baixa 20-30 s, média 10-15 s, alta 5-8 s).
 TimeSpan Gap(){int a=Level>=3?5000:Level==2?10000:20000,b=Level>=3?8000:Level==2?15000:30000;return TimeSpan.FromMilliseconds(rnd.Next(a,b));}
 bool Chance(double p){return rnd.NextDouble()<p;}
 string Pick(string[] list){return list[rnd.Next(list.Length)];}
 public bool IsBot(string name){return !String.IsNullOrEmpty(name)&&!Humans.Contains(name);}
 static string Weapon(string w){if(String.IsNullOrEmpty(w))return "tiro";if(w.StartsWith("U_"))w=w.Substring(2);return w.Replace('_',' ');}
 public static string Fill(string text,string k,string v,string w,int n,string h){return text.Replace("{k}",k??"").Replace("{v}",v??"").Replace("{w}",Weapon(w)).Replace("{n}",n.ToString()).Replace("{h}",h??"");}
 public static string Format(string who,string text){string s=who+": "+text; /* sem colchetes: o jogo já mostra [ADMINISTRADOR] antes (19/11) */return s.Length>127?s.Substring(0,127):s;}
 bool Ready(){return now()>=lastSay;}
 void Say(string who,string text,int delayMs){
  if(String.IsNullOrEmpty(who)||Level<=0)return;DateTime at=now().AddMilliseconds(delayMs);
  DateTime prev;if(lastBy.TryGetValue(who,out prev)&&at<prev.AddSeconds(6))return;lastBy[who]=at;
  queue.Add(new Line{At=at,Text=Format(who,text)});
 }
 void Commit(){lastSay=now()+Gap();}
 // Falas prontas para enviar agora.
 public List<string> Due(){var t=now();var due=queue.Where(l=>l.At<=t).OrderBy(l=>l.At).ToList();foreach(var l in due)queue.Remove(l);return due.Select(l=>l.Text).ToList();}
 public int Pending{get{return queue.Count;}}
 string OtherBot(params string[] not){var pool=Team.Keys.Where(n=>IsBot(n)&&!not.Contains(n,StringComparer.OrdinalIgnoreCase)).ToList();return pool.Count==0?null:pool[rnd.Next(pool.Count)];}
 public void OnKill(string killer,string victim,string weapon,bool headshot){
  if(String.IsNullOrEmpty(victim))return;
  int d;deaths.TryGetValue(victim,out d);deaths[victim]=d+1;kills[victim]=0;
  int kc=0;if(!String.IsNullOrEmpty(killer)&&!String.Equals(killer,victim,StringComparison.OrdinalIgnoreCase)){kills.TryGetValue(killer,out kc);kills[killer]=++kc;deaths[killer]=0;}
  if(Level<=0||!Ready())return;
  double scale=Level>=3?1.0:Level==2?0.7:0.45;
  string kk=killer,vv=victim;int vd=d+1;
  if(String.IsNullOrEmpty(killer)||String.Equals(killer,victim,StringComparison.OrdinalIgnoreCase)){if(IsBot(victim)&&Chance(0.5*scale)){Say(victim,Pick(ChatPhrases.Suicide),rnd.Next(800,1800));Commit();}return;}
  bool kb=IsBot(killer),vb=IsBot(victim);
  if(vb&&!kb&&Chance(0.55*scale)){Say(victim,Fill(Pick(ChatPhrases.VsHuman),kk,vv,weapon,vd,null),rnd.Next(900,2200));Commit();return;}
  if(kb&&!vb&&Chance(0.5*scale)){Say(killer,Fill(Pick(ChatPhrases.HumanTaunt),kk,vv,weapon,kc,null),rnd.Next(700,1600));Commit();return;}
  if(vb&&vd>=3&&Chance(0.6*scale)){Say(victim,Fill(Pick(ChatPhrases.Rage),kk,vv,weapon,vd,null),rnd.Next(900,2200));Commit();MaybeReply(victim,killer);return;}
  if(kb&&kc>=5&&kc%5==0&&Chance(0.7*scale)){Say(killer,Fill(Pick(ChatPhrases.Streak),kk,vv,weapon,kc,null),rnd.Next(700,1500));Commit();return;}
  if(headshot&&Chance(0.4*scale)){if(kb&&Chance(0.6))Say(killer,Fill(Pick(ChatPhrases.Headshot),kk,vv,weapon,kc,null),rnd.Next(700,1500));else if(vb)Say(victim,Fill(Pick(ChatPhrases.HeadshotVictim),kk,vv,weapon,vd,null),rnd.Next(900,2000));Commit();return;}
  if(kb&&Chance(0.12*scale)){Say(killer,Fill(Pick(ChatPhrases.Taunt),kk,vv,weapon,kc,null),rnd.Next(700,1600));Commit();MaybeReply(killer,victim);return;}
  if(vb&&Chance(0.08*scale)){Say(victim,Fill(Pick(ChatPhrases.Rage),kk,vv,weapon,vd,null),rnd.Next(900,2000));Commit();}
 }
 void MaybeReply(string first,string mention){if(Level<2||!Chance(Level>=3?0.35:0.2))return;string who=OtherBot(first,mention);if(who!=null)Say(who,Pick(ChatPhrases.Reply),rnd.Next(2600,4200));}
 public void OnChat(string who,string text,string subset){
  if(Level<=0||String.IsNullOrEmpty(who)||who=="Server"||IsBot(who))return;
  if(text==null||text.StartsWith("!")||text.StartsWith("ID_CHAT"))return;
  if(!Chance(Level>=3?0.8:Level==2?0.6:0.4))return;
  string t=text.ToLowerInvariant(),bot=OtherBot();if(bot==null)return;string[] pool;
  if(t.Contains("gg"))pool=ChatPhrases.HumanGg;else if(t.Contains("bot"))pool=ChatPhrases.HumanBot;
  else if(new[]{"porra","caralho","lixo","noob","merda","fdp","puta","vtnc","arrombad","otario","burro"}.Any(t.Contains))pool=ChatPhrases.HumanInsult;
  else if(t.Contains("kkk")||t.Contains("haha")||t.Contains("rsrs"))pool=ChatPhrases.HumanLaugh;else pool=ChatPhrases.HumanChatReply;
  Say(bot,Fill(Pick(pool),null,null,null,0,who),rnd.Next(1500,3200));
 }
 public void OnRoundOver(int winner){
  if(Level<=0)return;int count=Level>=3?10:Level==2?6:3;var bots=Team.Where(p=>IsBot(p.Key)).ToList();if(bots.Count==0)return;
  int delay=1200;var usedW=new HashSet<string>();var usedL=new HashSet<string>();
  for(int i=0;i<count;i++){bool win=i%2==0;var side=bots.Where(p=>(p.Value==winner)==win).ToList();if(side.Count==0)continue;var who=side[rnd.Next(side.Count)].Key;
   var list=win?ChatPhrases.Winner:ChatPhrases.Loser;var used=win?usedW:usedL;string text=Pick(list);for(int g=0;g<6&&used.Contains(text);g++)text=Pick(list);used.Add(text);
   lastBy.Remove(who);Say(who,text,delay);delay+=rnd.Next(700,1300);}
  lastSay=now().AddMilliseconds(delay+20000);deaths.Clear();kills.Clear();
 }
 // Conversa à toa de tempos em tempos (média e alta).
 public void Tick(){
  if(Level<2){return;}var t=now();if(nextCasual==DateTime.MinValue){nextCasual=t.AddSeconds(rnd.Next(40,80));return;}
  if(t<nextCasual||!Ready())return;nextCasual=t.AddSeconds(Level>=3?rnd.Next(60,100):rnd.Next(110,170));
  var talk=ChatPhrases.Casual[rnd.Next(ChatPhrases.Casual.Length)];var people=new List<string>();
  for(int i=0;i<talk.Length;i++){string who=i>=2&&people.Count>=2&&talk.Length==3?people[0]:OtherBot(people.ToArray());if(who==null)return;people.Add(who);}
  int delay=500;for(int i=0;i<talk.Length;i++){lastBy.Remove(people[i]);Say(people[i],talk[i],delay);delay+=rnd.Next(2500,4000);}Commit();
 }
 public void SetPlayers(IEnumerable<KeyValuePair<string,int>> team,IEnumerable<string> humans){Team.Clear();foreach(var p in team)Team[p.Key]=p.Value;Humans.Clear();foreach(var h in humans)Humans.Add(h);}
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
   say.Connect(host,port,pw);Players(brain,say);DateTime nextPlayers=DateTime.UtcNow.AddSeconds(30);
   LastStatus="Chat em português ativo ("+host+").";
   while(Enabled&&Level>0){
    brain.Level=Level;
    if(events.Client.Poll(1000000,SelectMode.SelectRead)){
     uint flags;var words=Read(s,out flags);if(words==null)throw new IOException("conexão encerrada");
     if((flags&0x40000000)==0){var reply=Rcon.Encode((flags&0x3fffffff)|0x40000000,new[]{"OK"});s.Write(reply,0,reply.Length);Handle(brain,say,words);}
    }
    if(DateTime.UtcNow>=nextPlayers){Players(brain,say);nextPlayers=DateTime.UtcNow.AddSeconds(30);}
    brain.Tick();
    foreach(var line in brain.Due())say.Command("admin.say",line,"all");
   }
  }
 }
 static void Handle(ChatBrain brain,Rcon say,string[] w){
  if(w.Length==0)return;
  switch(w[0]){
   case "player.onKill":if(w.Length>=5)brain.OnKill(w[1],w[2],w[3],w[4]=="true");break;
   case "player.onChat":if(w.Length>=3)brain.OnChat(w[1],w[2],w.Length>3?w[3]:"all");break;
   case "server.onRoundOver":{int team;Players(brain,say);if(w.Length>=2&&Int32.TryParse(w[1],out team))brain.OnRoundOver(team);break;}
   case "player.onJoin":case "player.onLeave":Players(brain,say);break;
  }
 }
 // Bot = sem GUID e ping 65535 (como o mod mostra no placar); o resto é jogador de verdade.
 static void Players(ChatBrain brain,Rcon say){
  var p=say.Command("listPlayers","all");int n;if(p.Length<3||!Int32.TryParse(p[1],out n)||n<1)return;var cols=p.Skip(2).Take(n).ToList();int count;if(!Int32.TryParse(p[2+n],out count))return;
  int iName=cols.IndexOf("name"),iGuid=cols.IndexOf("guid"),iTeam=cols.IndexOf("teamId"),iPing=cols.IndexOf("ping");if(iName<0)return;
  var team=new List<KeyValuePair<string,int>>();var humans=new List<string>();
  for(int r=0;r<count;r++){int b=3+n+r*n;if(b+n>p.Length)break;string name=p[b+iName];int t=0;if(iTeam>=0)Int32.TryParse(p[b+iTeam],out t);team.Add(new KeyValuePair<string,int>(name,t));
   bool bot=(iGuid<0||p[b+iGuid]=="")&&(iPing<0||p[b+iPing]=="65535");if(!bot)humans.Add(name);}
  brain.SetPlayers(team,humans);
 }
 static string[] Ask(NetworkStream s,uint id,params string[] words){var b=Rcon.Encode(id,words);s.Write(b,0,b.Length);for(int i=0;i<64;i++){uint f;var r=Read(s,out f);if(r==null)throw new IOException("conexão encerrada");if((f&0x40000000)!=0&&(f&0x3fffffff)==id)return r;}throw new IOException("sem resposta ao comando");}
 static string[] Read(NetworkStream s,out uint flags){flags=0;var h=Exact(s,12);if(h==null)return null;uint size=BitConverter.ToUInt32(h,4);if(size<12||size>2*1024*1024)throw new InvalidDataException("pacote RCON fora do limite");var body=Exact(s,(int)size-12);if(body==null)return null;flags=BitConverter.ToUInt32(h,0);return Rcon.Decode(h.Concat(body).ToArray());}
 static byte[] Exact(NetworkStream s,int count){var b=new byte[count];int o=0;while(o<count){int n=s.Read(b,o,count-o);if(n<=0)return null;o+=n;}return b;}
}
}
