using System;
using System.Collections.Generic;
using System.Linq;
namespace BF4Bots {
// Chat em português: regras do ChatBrain com relógio e sorteio fixos (sem servidor).
public static class ChatTests {
 public static string Run(){
  var t=new DateTime(2026,11,19,12,0,0);var brain=new ChatBrain(7,()=>t){Level=3};
  var team=new List<KeyValuePair<string,int>>();for(int i=0;i<10;i++)team.Add(new KeyValuePair<string,int>("Bot"+i,i%2+1));team.Add(new KeyValuePair<string,int>("Jogador",1));
  brain.SetPlayers(team,new[]{"Jogador"});
  if(brain.IsBot("Jogador")||!brain.IsBot("Bot3"))throw new Exception("Chat: humano/bot trocados");
  var said=new List<string>();
  for(int i=0;i<400;i++){brain.OnKill("Bot"+(i%5*2),"Bot"+(i%5*2+1),"U_AWS",i%7==0);if(i%9==0)brain.OnKill("Bot2","Jogador","U_M320_HE",false);if(i%11==0)brain.OnKill("Jogador","Bot5","U_AK12",true);brain.Tick();t=t.AddSeconds(2);said.AddRange(brain.Due());}
  if(said.Count<20)throw new Exception("Chat: poucas falas ("+said.Count+")");
  if(said.Any(l=>l.Length>127||!l.StartsWith("[")||l.Contains("{")))throw new Exception("Chat: fala mal formada");
  if(said.Any(l=>l.StartsWith("[Jogador]")))throw new Exception("Chat: falou em nome do jogador");
  // Intervalo mínimo: em 800 s na intensidade alta (5-8 s) não pode passar de ~170 falas de morte.
  if(said.Count>260)throw new Exception("Chat: falas demais ("+said.Count+")");
  brain.OnChat("Jogador","gg time","all");t=t.AddSeconds(4);var reply=brain.Due();
  brain.OnRoundOver(1);for(int i=0;i<30;i++){t=t.AddSeconds(1);reply.AddRange(brain.Due());}
  if(reply.Count<5)throw new Exception("Chat: fim de rodada/resposta sem falas");
  var off=new ChatBrain(1,()=>t){Level=0};off.SetPlayers(team,new[]{"Jogador"});for(int i=0;i<50;i++){off.OnKill("Bot1","Bot2","x",true);off.OnRoundOver(1);off.Tick();}
  if(off.Pending>0)throw new Exception("Chat: falou com intensidade desligada");
  foreach(var list in new[]{ChatPhrases.Rage,ChatPhrases.Taunt,ChatPhrases.Winner,ChatPhrases.Loser,ChatPhrases.HumanChatReply})if(list.Any(x=>x.Any(c=>c>127)))throw new Exception("Chat: frase com acento");
  return "PASS chat em português: bot x humano, intervalo, formato, resposta ao jogador, fim de rodada e desligado ("+said.Count+" falas em 800 s)";
 }
}
}
