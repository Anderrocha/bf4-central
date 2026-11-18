using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
// "PC servidor": a Central pede ao painel BF4 Servidor (outro PC da rede) para ligar o servidor com bots.
// O dono do outro PC aceita ou recusa numa janela. Aceitou: a Central aponta o jogo para lá e o
// processador deste PC fica todo para o jogo. Recusou (ou ninguém respondeu): tudo volta para este PC.
// Protocolo: veja servidor\Pedidos.cs.
namespace BF4Bots {
public sealed class RemoteRefused : Exception { public RemoteRefused(string message):base(message){} }
public static class RemotePc {
 public const int ControlPort=25230,DiscoveryPort=25231;
 static string Folder{get{string f=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BF4BotLauncher");Directory.CreateDirectory(f);return f;}}
 static string Read(string file){try{string f=Path.Combine(Folder,file);return File.Exists(f)?File.ReadAllText(f).Trim():null;}catch{return null;}}
 public static string SavedHost(){string h=Read("remote-server.txt");return String.IsNullOrEmpty(h)?null:h;}
 // Endereço do PC servidor em uso (só existe enquanto a Central está conectada a ele).
 public static string ActiveHost(){string h=Read("remote-active.txt");return String.IsNullOrEmpty(h)?null:h;}
 public static void SetActive(string host){File.WriteAllText(Path.Combine(Folder,"remote-server.txt"),host);File.WriteAllText(Path.Combine(Folder,"remote-active.txt"),host);}
 public static void ClearActive(){try{File.Delete(Path.Combine(Folder,"remote-active.txt"));}catch{}}
 static TcpClient Open(string host,int port,int ms){
  var client=new TcpClient();
  try{var ar=client.BeginConnect(host,port,null,null);if(!ar.AsyncWaitHandle.WaitOne(ms))throw new IOException("Sem resposta de "+host+".");client.EndConnect(ar);return client;}
  catch{client.Close();throw;}
 }
 public static bool Reachable(string host,int port,int ms){try{using(Open(host,port,ms))return true;}catch{return false;}}
 // Procura painéis na rede (broadcast UDP). Devolve os IPs que responderam aceitando pedidos.
 public static List<string> Discover(int ms){
  var found=new List<string>();
  using(var udp=new UdpClient(0){EnableBroadcast=true}){
   var ask=Encoding.UTF8.GetBytes("BF4CENTRAL?");
   var targets=new List<IPAddress>{IPAddress.Broadcast};
   try{foreach(var nic in NetworkInterface.GetAllNetworkInterfaces().Where(n=>n.OperationalStatus==OperationalStatus.Up&&n.NetworkInterfaceType!=NetworkInterfaceType.Loopback))foreach(var u in nic.GetIPProperties().UnicastAddresses)if(u.Address.AddressFamily==AddressFamily.InterNetwork&&u.IPv4Mask!=null){var a=u.Address.GetAddressBytes();var m=u.IPv4Mask.GetAddressBytes();var b=new byte[4];for(int i=0;i<4;i++)b[i]=(byte)(a[i]|~m[i]);targets.Add(new IPAddress(b));}}catch{}
   foreach(var t in targets.Distinct())try{udp.Send(ask,ask.Length,new IPEndPoint(t,DiscoveryPort));}catch{}
   var end=DateTime.UtcNow.AddMilliseconds(ms);
   while(true){
    int left=(int)(end-DateTime.UtcNow).TotalMilliseconds;if(left<=0)break;udp.Client.ReceiveTimeout=left;
    try{var from=new IPEndPoint(IPAddress.Any,0);var parts=Encoding.UTF8.GetString(udp.Receive(ref from)).Trim().Split('|');if(parts.Length>=4&&parts[0]=="BF4SERVIDOR"&&parts[3]=="1"&&!found.Contains(from.Address.ToString()))found.Add(from.Address.ToString());}
    catch(SocketException){break;}
   }
  }
  return found;
 }
 // O último PC servidor usado; se não responder, procura na rede.
 public static string Find(Action<string> progress){
  string saved=SavedHost();
  if(saved!=null&&Reachable(saved,ControlPort,1500))return saved;
  progress("Procurando o painel BF4 Servidor na rede…");
  var list=Discover(2000);if(list.Count==0)list=Discover(2500);
  return list.Count>0?list[0]:null;
 }
 // Faz o pedido e espera: o dono aceitar (até ~60 s) e o servidor ficar pronto (até ~3 min).
 // Devolve {ip, portaRcon}. RemoteRefused quando recusou, não respondeu ou desativou os pedidos.
 public static string[] Request(string host,string who,Action<string> progress){
  TcpClient client;
  try{client=Open(host,ControlPort,3000);}catch{throw new RemoteRefused("O PC servidor ("+host+") não respondeu. O painel BF4 Servidor está aberto lá?");}
  using(client){
   var stream=client.GetStream();client.ReceiveTimeout=90000;
   var writer=new StreamWriter(stream,new UTF8Encoding(false)){AutoFlush=true,NewLine="\n"};var reader=new StreamReader(stream,new UTF8Encoding(false));
   writer.WriteLine("BF4CENTRAL|1|CONNECT|"+who.Replace("|","/"));
   while(true){
    string line;
    try{line=reader.ReadLine();}catch(IOException){throw new RemoteRefused("O outro PC parou de responder ao pedido.");}
    if(line==null)throw new RemoteRefused("O outro PC encerrou o pedido sem responder.");
    var p=line.Split('|');
    switch(p[0]){
     case "WAIT":progress("Pedido enviado para o PC servidor ("+host+"). Aguardando o dono aceitar…");break;
     case "DENIED":throw new RemoteRefused(p.Length>1?p[1]:"O outro PC recusou.");
     case "ACCEPTED":client.ReceiveTimeout=200000;progress("Pedido aceito! Ligando o servidor no outro PC (até 2 minutos)…");break;
     case "STATUS":if(p.Length>1)progress("PC servidor: "+p[1]);break;
     case "READY":if(p.Length<3)throw new IOException("Resposta inválida do PC servidor.");return new[]{p[1],p[2]};
     case "FAILED":throw new IOException("O PC servidor não conseguiu ligar: "+(p.Length>1?p[1]:"erro desconhecido"));
    }
   }
  }
 }
 // Avisa o painel que este PC não precisa mais do servidor (ele desliga o que ligou a pedido).
 public static void Release(string host,string who){
  try{using(var client=Open(host,ControlPort,2000)){client.ReceiveTimeout=30000;var stream=client.GetStream();var writer=new StreamWriter(stream,new UTF8Encoding(false)){AutoFlush=true,NewLine="\n"};writer.WriteLine("BF4CENTRAL|1|RELEASE|"+who.Replace("|","/"));new StreamReader(stream).ReadLine();}}catch{}
 }
}
public static partial class OneClick {
 public static void StopLocalBackend(string root){string backend=Path.Combine(root,"Server\\BF4BlazeServer.exe");foreach(var p in At(backend))CloseVerified(p,backend);}
}
public sealed partial class Central {
 string RemoteWho(){string name=user!=null&&user.Text.Trim().Length>0?user.Text.Trim():"Jogador";return name+" ("+Environment.MachineName+")";}
 bool PointsAway(){var c=Settings.Read(root);return Settings.Get(c,"BlazeIP","127.0.0.1")!="127.0.0.1"||Settings.Get(c,"RconHost","127.0.0.1")!="127.0.0.1";}
 // Volta jogo e controle para o servidor deste PC (o jogo já foi fechado ou confirmado antes).
 void BackToLocal(){
  string host=RemotePc.ActiveHost();if(host!=null){RemotePc.Release(host,RemoteWho());RemotePc.ClearActive();}
  if(PointsAway()){Friends.ReturnLocal(root);Settings.Save(root,new Dictionary<string,string>{{"RconHost","127.0.0.1"}});}
 }
 // "Voltar ao meu servidor": libera o PC servidor (se houver) e aponta jogo e controle para este PC.
 void ReturnLocalAll(){string host=RemotePc.ActiveHost();if(host!=null){RemotePc.Release(host,RemoteWho());RemotePc.ClearActive();}Friends.ReturnLocal(root);Settings.Save(root,new Dictionary<string,string>{{"RconHost","127.0.0.1"}});}
 const string LocalAdvice=" Sem a divisão do processador, use menos bots e mapas fechados (ex.: Operation Locker) para não pesar.";
 async Task ConnectRemote(){
  if(!ConfirmGameSwitch())return;
  bool ready=false;
  await Work("Procurando o PC servidor…",async()=>{
   var progress=Progress();string who=RemoteWho(),refusal=null;string[] result=null;string host=null;
   await Task.Run(()=>{
    host=RemotePc.Find(progress);
    if(host==null){refusal="Nenhum PC com o painel BF4 Servidor respondeu na rede.";return;}
    try{result=RemotePc.Request(host,who,progress);}catch(RemoteRefused ex){refusal=ex.Message;}
   });
   if(refusal!=null){
    await Task.Run(()=>BackToLocal());LoadSettings();
    Status(refusal+" Voltei tudo para o servidor deste PC."+LocalAdvice,true);return;
   }
   string ip=result[0],rconPort=result[1];
   await Task.Run(()=>{
    OneClick.CloseGameForSwitch(root);
    OneClick.StopLocalServer(root);
    OneClick.StopLocalBackend(root);
    OneClick.ReloadOriginal(root);
    Settings.Save(root,new Dictionary<string,string>{{"BlazeIP",ip},{"RconHost",ip},{"RconPort",rconPort}});
    RemotePc.SetActive(ip);
   });
   LoadSettings();Status("Conectado ao PC servidor "+ip+". O processador deste PC fica todo para o jogo. Abrindo o jogo…");ready=true;
  });
  if(ready)await StartAll();
 }
 // Antes de abrir o jogo: se a Central aponta para um PC servidor que não está mais ligado, volta para este PC.
 async Task<bool> CheckRemoteBeforeStart(){
  string host=RemotePc.ActiveHost();if(host==null)return true;
  var c=Settings.Read(root);if(Settings.Get(c,"BlazeIP","127.0.0.1")!=host){RemotePc.ClearActive();return true;}
  if(await Task.Run(()=>RemotePc.Reachable(host,10040,2000)))return true;
  try{await Task.Run(()=>{RemotePc.ClearActive();if(!OneClick.IsGameOpen(root)&&PointsAway()){Friends.ReturnLocal(root);Settings.Save(root,new Dictionary<string,string>{{"RconHost","127.0.0.1"}});}});LoadSettings();}catch(Exception ex){Status(ex.Message,true);return false;}
  Status("O PC servidor ("+host+") não está ligado. Voltei para o servidor deste PC."+LocalAdvice,true);
  return true;
 }
}
}
