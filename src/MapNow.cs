using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
namespace BF4Bots {public static class MapNow {
 public static List<RotationEntry> Exclusive(RotationEntry choice){return new List<RotationEntry>{new RotationEntry{Map=choice.Map,Mode=choice.Mode,Rounds=choice.Rounds}};}
 public static List<RotationEntry> First(List<RotationEntry> current,RotationEntry chosen){var result=current.Where(x=>x.Map!=chosen.Map||x.Mode!=chosen.Mode).Select(x=>new RotationEntry{Map=x.Map,Mode=x.Mode,Rounds=x.Rounds}).ToList();result.Insert(0,new RotationEntry{Map=chosen.Map,Mode=chosen.Mode,Rounds=chosen.Rounds});return result;}
 public static void SwitchAndWait(Rcon connection,RotationEntry choice,string name,int index=0){var initial=connection.Command("serverInfo");if(initial.Length<=5||initial[5]!=choice.Map||initial[4]!=choice.Mode){connection.Command("mapList.setNextMapIndex",index.ToString());connection.Command("mapList.runNextRound");}var timer=Stopwatch.StartNew();int stable=0;while(timer.Elapsed.TotalSeconds<90){string[] info;try{info=connection.Command("serverInfo");}catch(Exception ex){if(!(ex is IOException)&&!(ex is SocketException))throw;stable=0;Thread.Sleep(750);try{connection.Reconnect();}catch(IOException){}catch(SocketException){}continue;}if(info.Length>5&&info[5]==choice.Map&&info[4]==choice.Mode){if(++stable>=4){connection.Command("vars.serverName",name);var final=connection.Command("serverInfo");if(final.Length>5&&final[1]==name&&final[4]==choice.Mode&&final[5]==choice.Map)return;throw new IOException("O servidor não confirmou o nome e o mapa escolhidos.");}}else stable=0;Thread.Sleep(750);}throw new TimeoutException("A troca foi solicitada, mas o servidor não confirmou o mapa em 90 segundos. Não informei sucesso; atualize a conexão e confira o servidor.");}
}}
