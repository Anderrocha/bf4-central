using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace BF4Bots {
// Depois de "Conectado": acompanha a partida e tira as etapas da barra quando o jogador sai dela ou fecha o jogo.
public enum MatchState { Unknown, InMatch, Left, Closed }
public static partial class OneClick {
 // Closed: o jogo não está aberto. Left/InMatch: o nome aparece (ou não) na lista de jogadores do servidor (RCON).
 // Unknown: não deu para consultar o servidor (sem RCON, senha errada, servidor remoto...).
 public static MatchState CheckMatch(string root,string player,string host,int port,string password){
  if(!IsGameOpen(root))return MatchState.Closed;
  if(String.IsNullOrWhiteSpace(player)||String.IsNullOrWhiteSpace(host))return MatchState.Unknown;
  try{
   using(var rcon=new Rcon()){
    rcon.Connect(host,port,password);
    var players=Friends.Players(rcon.Command("admin.listPlayers","all"));
    return InMatch(players,player)?MatchState.InMatch:MatchState.Left;
   }
  }catch{return MatchState.Unknown;}
 }
 public static bool InMatch(IEnumerable<PartyPlayer> players,string player){return players.Any(p=>String.Equals((p.Name??"").Trim(),player.Trim(),StringComparison.OrdinalIgnoreCase));}
}
public sealed partial class Central {
 Timer matchTimer;List<Step> watchedSteps;bool matchSeen,matchChecking,matchLeft;
 void StartMatchWatch(){
  if(!tracker.HasSteps)return;
  watchedSteps=tracker.Steps;matchSeen=false;matchLeft=false;
  if(matchTimer==null){matchTimer=new Timer{Interval=5000};matchTimer.Tick+=async(s,e)=>await MatchTick();FormClosed+=(s,e)=>matchTimer.Dispose();}
  matchTimer.Start();
 }
 void StopMatchWatch(){if(matchTimer!=null)matchTimer.Stop();watchedSteps=null;}
 async Task MatchTick(){
  if(matchChecking||busy||IsDisposed)return;
  // Outra operação trocou as etapas (ex.: carregar mapa): não mexe nelas.
  if(!matchLeft&&!ReferenceEquals(tracker.Steps,watchedSteps)){StopMatchWatch();return;}
  matchChecking=true;
  try{
   string player=user.Text.Trim(),host=rconHost.Text.Trim(),secret=password.Text;int p=(int)port.Value;
   var state=await Task.Run(()=>OneClick.CheckMatch(root,player,host,p,secret));
   if(IsDisposed||busy)return;
   if(state==MatchState.Closed){
    tracker.Clear();tracker.Report("O Battlefield 4 foi fechado. Clique em Iniciar tudo e jogar para entrar de novo.",StatusKind.Info);StopMatchWatch();
   }else if(state==MatchState.InMatch){
    matchSeen=true;
   }else if(state==MatchState.Left&&matchSeen&&!matchLeft){
    // Só considera "saiu" depois de ter visto o jogador na partida (evita o tempo de carregamento).
    matchLeft=true;tracker.Clear();tracker.Report("Você saiu da partida. O jogo continua aberto; entre de novo pelo menu do BF4 ou clique em Iniciar tudo e jogar.",StatusKind.Info);
   }
  }finally{matchChecking=false;}
 }
}
}
