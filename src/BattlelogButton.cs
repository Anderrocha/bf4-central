using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows.Forms;
namespace BF4Bots {
// Battlelog local do backend do mod: endereço e se está no ar.
public static partial class OneClick {
 public static int BattlelogPort(string root){try{return Port(File.ReadAllText(Path.Combine(root,"Server\\config.txt")),"HttpPort",80);}catch{return 80;}}
 public static string BattlelogUrl(int port){return "http://127.0.0.1"+(port==80?"":":"+port)+"/bf4/en/";}
 // Só considera no ar quando o próprio backend desta instalação escuta a porta (evita abrir outro programa na porta 80).
 public static bool BattlelogOnline(string root,int port){
  try{
   bool listening=IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Any(e=>e.Port==port&&(IPAddress.IsLoopback(e.Address)||e.Address.Equals(IPAddress.Any)));
   return listening&&At(Path.Combine(root,"Server\\BF4BlazeServer.exe")).Count>0;
  }catch{return false;}
 }
}
public sealed partial class Central {
 Button battlelog;Timer battlelogTimer;
 Button BattlelogButton(){
  battlelog=Button("Abrir Battlelog",OpenBattlelog);
  battlelogTimer=new Timer{Interval=3000};
  Action refresh=()=>{if(IsDisposed||battlelog.IsDisposed)return;bool online=OneClick.BattlelogOnline(root,OneClick.BattlelogPort(root));if(battlelog.Enabled!=online)battlelog.Enabled=online;};
  battlelogTimer.Tick+=(s,e)=>refresh();battlelogTimer.Start();refresh();
  FormClosed+=(s,e)=>battlelogTimer.Dispose();
  return battlelog;
 }
 void OpenBattlelog(){
  int port=OneClick.BattlelogPort(root);
  if(!OneClick.BattlelogOnline(root,port))throw new InvalidOperationException("O Battlelog local só abre com o servidor offline no ar. Clique em Iniciar servidor local.");
  Process.Start(new ProcessStartInfo(OneClick.BattlelogUrl(port)){UseShellExecute=true});
  Status("Battlelog local aberto no navegador. Para entrar, use o seu nome de jogador e deixe a senha em branco.");
 }
}
}
