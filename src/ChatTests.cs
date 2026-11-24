using System;
using System.Collections.Generic;
using System.Linq;
namespace BF4Bots {
// Chat em português: regras do ChatBrain com relógio e sorteio fixos (sem servidor).
public static class ChatTests {
 public static string Run(){
  var t=new DateTime(2026,11,19,12,0,0);var brain=new ChatBrain(7,()=>t){Level=3};
  var team=new List<KeyValuePair<string,int>>();var squads=new List<KeyValuePair<string,int>>();
  for(int i=0;i<16;i++){team.Add(new KeyValuePair<string,int>("Bot"+i,i%2+1));squads.Add(new KeyValuePair<string,int>("Bot"+i,i<4?1:2));}
  team.Add(new KeyValuePair<string,int>("Jogador",1));squads.Add(new KeyValuePair<string,int>("Jogador",1));
  brain.SetPlayers(team,new[]{"Jogador"},squads);
  if(brain.IsBot("Jogador")||!brain.IsBot("Bot3"))throw new Exception("Chat: humano/bot trocados");
  var lines=new List<ChatBrain.Line>();
  for(int i=0;i<400;i++){brain.OnKill("Bot"+(i%5*2),"Bot"+(i%5*2+1),"U_AWS",i%7==0);if(i%9==0)brain.OnKill("Bot3","Jogador","U_M320_HE",false);if(i%3==0)brain.OnKill("Jogador","Bot"+(i%4*2+1),"U_AK12",i%2==0);
   if(i%50==10)brain.OnScores(300,90);brain.Tick();t=t.AddSeconds(2);lines.AddRange(brain.DueLines());}
  var said=lines.Select(l=>l.Text).ToList();
  if(said.Count<120)throw new Exception("Chat: poucas falas ("+said.Count+")");
  if(said.Any(l=>l.Length>127||!l.Contains(": ")||(l.StartsWith("[")&&!l.StartsWith("[TIME] "))||l.Contains("{")))throw new Exception("Chat: fala mal formada: "+said.First(l=>l.Length>127||!l.Contains(": ")||(l.StartsWith("[")&&!l.StartsWith("[TIME] "))||l.Contains("{")));
  if(said.Any(l=>l.StartsWith("Jogador:")))throw new Exception("Chat: falou em nome do jogador");
  if(said.Count>900)throw new Exception("Chat: falas demais ("+said.Count+")");
  // Denúncia, IA e votação aparecem quando o jogador domina a partida.
  if(!said.Any(l=>l.Contains("report")))throw new Exception("Chat: ninguém pediu report");
  if(!said.Any(l=>l.StartsWith(ChatBrain.VoteName+":")))throw new Exception("Chat: votação sem resultado");
  if(!said.Any(l=>l.StartsWith(ChatBrain.AiName+":")))throw new Exception("Chat: IA anticheat não apareceu");
  // Cores: o time do jogador fala às vezes no canal do time/esquadrão; o inimigo só no geral.
  if(!lines.Any(l=>l.Team==1))throw new Exception("Chat: time do jogador nunca usou o canal do time");
  if(!lines.Any(l=>l.Squad==1))throw new Exception("Chat: esquadrão do jogador nunca usou o canal do esquadrão");
  if(lines.Any(l=>l.Team==2))throw new Exception("Chat: time inimigo falou num canal que o jogador não vê");
  // [TIME] só para bots do time do jogador (time 1).
  foreach(var l in lines.Where(x=>x.Who!=null&&x.Who.StartsWith("Bot"))){int n=Int32.Parse(l.Who.Substring(3));bool ally=n%2==0;if(ally!=l.Text.StartsWith("[TIME] "))throw new Exception("Chat: [TIME] errado: "+l.Text);}
  // Assunto em cadeia: alguém pergunta e 2+ colegas respondem em sequência.
  bool chain=false;foreach(var th in ChatPhrases.Threads)foreach(var o in th.Open){int i=said.FindIndex(x=>x.EndsWith(": "+o));if(i<0)continue;int rc=said.Skip(i+1).Take(6).Count(x=>th.Replies.Any(rp=>x.Contains(": "+rp.Replace("{f}","").Split(' ')[0])));if(rc>=2)chain=true;}
  if(!chain)throw new Exception("Chat: nenhuma conversa com 2+ respostas");
  // Repetição: na mesma janela de 30 falas, quase nada pode se repetir (fora "1"/"2" da votação e risadas curtas).
  int rep=0;for(int i=0;i<said.Count;i++){var body=said[i].Substring(said[i].IndexOf(": ")+2);if(body.Length<6)continue;for(int j=Math.Max(0,i-30);j<i;j++)if(said[j].EndsWith(": "+body)){rep++;break;}}
  if(rep*10>said.Count)throw new Exception("Chat: repetição alta ("+rep+" de "+said.Count+")");
  // Voto do jogador é contado e a votação encerra.
  var vb=new ChatBrain(3,()=>t){Level=3};vb.SetPlayers(team,new[]{"Jogador"},squads);
  for(int i=0;i<200&&!vb.VoteOpen;i++){vb.OnKill("Bot4","Bot5","x",false);t=t.AddSeconds(3);vb.Tick();vb.Due();}
  if(!vb.VoteOpen)throw new Exception("Chat: bot que só morre nunca virou votação");
  vb.OnChat("Jogador","1","all");var r=new List<string>();for(int i=0;i<30;i++){t=t.AddSeconds(1);vb.Tick();r.AddRange(vb.Due());}
  if(!r.Any(l=>l.StartsWith(ChatBrain.VoteName+":")))throw new Exception("Chat: votação aberta não encerrou");
  if(!r.Any(l=>l.Contains("Jogador")))throw new Exception("Chat: voto do jogador não foi comentado");
  brain.OnChat("Jogador","gg time","all");t=t.AddSeconds(4);var reply=brain.Due();
  // Fim de partida: resumo com o placar real e silêncio de combate depois.
  brain.SetScore("Bot4",101,12);brain.SetScore("Jogador",40,22);brain.SetScore("Bot7",3,30);brain.SetScore("Bot4",0,0);
  brain.OnRoundOver(1);for(int i=0;i<40;i++){t=t.AddSeconds(1);brain.Tick();reply.AddRange(brain.Due());}
  if(reply.Count<8)throw new Exception("Chat: fim de rodada/resposta sem falas");
  if(!reply.Any(l=>l.Contains("Bot4")&&l.Contains("101")))throw new Exception("Chat: resumo sem o mvp e as kills reais");
  if(!reply.Any(l=>l.Contains("Jogador")&&(l.Contains("40")||l.Contains("22"))))throw new Exception("Chat: resumo sem o jogador de verdade");
  var after=new List<string>();for(int i=0;i<120;i++){t=t.AddSeconds(1);brain.Tick();brain.OnScores(500,10);after.AddRange(brain.Due());}
  if(after.Count>0)throw new Exception("Chat: falou de combate depois do fim da partida: "+after[0]);
  // Mapa fechado (Locker): nada de tanque, heli, carro...
  var lk=new ChatBrain(11,()=>t){Level=3,Map="MP_Prison"};lk.SetPlayers(team,new[]{"Jogador"},squads);var lks=new List<string>();
  for(int i=0;i<600;i++){lk.OnKill("Bot"+(i%5*2),"Bot"+(i%5*2+1),"U_AWS",i%7==0);if(i%3==0)lk.OnKill("Jogador","Bot"+(i%4*2+1),"U_AK12",false);lk.Tick();t=t.AddSeconds(2);lks.AddRange(lk.Due());}
  var veh=lks.FirstOrDefault(l=>ChatPhrases.Vehicle.IsMatch(l.Substring(l.IndexOf(": ")+2)));if(veh!=null)throw new Exception("Chat: veículo no Locker: "+veh);
  lk.OnLevelLoaded("MP_Prison");if(lk.RoundOver)throw new Exception("Chat: nova partida não voltou a conversar");
  var off=new ChatBrain(1,()=>t){Level=0};off.SetPlayers(team,new[]{"Jogador"});for(int i=0;i<50;i++){off.OnKill("Bot1","Bot2","x",true);off.OnKill("Jogador","Bot2","x",true);off.OnRoundOver(1);off.Tick();}
  if(off.Pending>0)throw new Exception("Chat: falou com intensidade desligada");
  var all=typeof(ChatPhrases).GetFields().Where(f=>f.FieldType==typeof(string[])).SelectMany(f=>(string[])f.GetValue(null)).Concat(ChatPhrases.Casual.SelectMany(x=>x)).ToList();
  if(all.Any(x=>x.Any(c=>c>127)))throw new Exception("Chat: frase com acento: "+all.First(x=>x.Any(c=>c>127)));
  return "PASS chat em português: "+all.Count+" frases, report/IA/votação, canal de time e esquadrão, pouca repetição ("+rep+"), voto do jogador, fim de rodada e desligado ("+said.Count+" falas em 800 s)";
 }
}
}
