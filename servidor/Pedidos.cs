using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
// Pedidos da BF4 Central pela rede de casa.
// Protocolo (uma linha de texto por mensagem, UTF-8):
//   UDP 25231  Central → "BF4CENTRAL?"            painel → "BF4SERVIDOR|nome-do-pc|ip|aceita(1/0)"
//   TCP 25230  Central → "BF4CENTRAL|1|CONNECT|quem"   painel → "WAIT", depois "DENIED|motivo"
//                                                       ou "ACCEPTED", "STATUS|texto"…, "READY|ip|portaRcon" / "FAILED|motivo"
//              Central → "BF4CENTRAL|1|RELEASE|quem"   painel → "OK|texto"
//              Central → "BF4CENTRAL|1|PING|quem"      painel → "PONG|nome-do-pc|estado"
//              Central → "BF4CENTRAL|1|LOGS|quem"      painel → "LOGS|linhas separadas por " (só leitura)
// Nada liga sem o "Aceitar" do dono deste PC (ou "Sempre aceitar" marcado por ele para aquele PC).
namespace BF4Servidor {
public sealed class Pedidos : IDisposable {
 public const int ControlPort=25230,DiscoveryPort=25231;
 public Func<string,string,bool> Ask;           // (quem, ip) → aceitou?  (roda na thread da janela)
 public Func<Action<string>,string> StartServer;// liga e devolve "ip|portaRcon"
 public Func<bool> StopIfOurs;                  // desliga se foi ligado por pedido; devolve se desligou
 public Func<string> State;
 public Func<string,bool> ApplyHook;              // grava os ajustes dos bots recebidos (só chaves conhecidas); true = mudou
 public Func<Action<string>,string> RestartServer;// reinicia só o servidor (para valer os ajustes)
 volatile string lastAccepted;                   // IP do PC de jogo que teve o pedido aceito
 public string LastAccepted{get{return lastAccepted;}set{lastAccepted=value;}}
 public Action<string> Accepted;                 // guarda o IP aceito (sobrevive a reinício do painel)
 public Func<string,string> WriteMapList;         // grava Instance\Admin\MapList.txt; devolve o conteúdo anterior
 public Func<string> Logs;                      // final dos logs do backend/servidor (diagnóstico pela Central)
 public Action<string> Log;
 public volatile bool Accepting=true;
 TcpListener tcp;UdpClient udp;Thread tcpThread,udpThread;volatile bool closed;int handling;
 public void Start(){
  tcp=new TcpListener(IPAddress.Any,ControlPort);tcp.Start();
  tcpThread=new Thread(TcpLoop){IsBackground=true,Name="pedidos-tcp"};tcpThread.Start();
  try{udp=new UdpClient(new IPEndPoint(IPAddress.Any,DiscoveryPort));udpThread=new Thread(UdpLoop){IsBackground=true,Name="pedidos-udp"};udpThread.Start();}catch(Exception ex){Say("A busca automática não abriu (porta "+DiscoveryPort+"): "+ex.Message);}
 }
 void Say(string t){var l=Log;if(l!=null)l(t);}
 void UdpLoop(){
  while(!closed){
   try{var from=new IPEndPoint(IPAddress.Any,0);var data=udp.Receive(ref from);if(Encoding.UTF8.GetString(data).Trim()!="BF4CENTRAL?")continue;
    var reply=Encoding.UTF8.GetBytes("BF4SERVIDOR|"+Environment.MachineName+"|"+(Engine.LanIp()??"")+"|"+(Accepting?"1":"0"));udp.Send(reply,reply.Length,from);}
   catch{if(closed)return;Thread.Sleep(200);}
  }
 }
 void TcpLoop(){
  while(!closed){
   TcpClient client;try{client=tcp.AcceptTcpClient();}catch{if(closed)return;Thread.Sleep(200);continue;}
   var t=new Thread(()=>Handle(client)){IsBackground=true};t.Start();
  }
 }
 void Handle(TcpClient client){
  using(client){
   try{
    client.ReceiveTimeout=8000;client.SendTimeout=8000;var stream=client.GetStream();var reader=new StreamReader(stream,new UTF8Encoding(false));var writer=new StreamWriter(stream,new UTF8Encoding(false)){AutoFlush=true,NewLine="\n"};
    string line=reader.ReadLine();if(line==null)return;var parts=line.Split('|');
    if(parts.Length<4||parts[0]!="BF4CENTRAL"){writer.WriteLine("FAILED|Pedido desconhecido.");return;}
    string who=Clean(parts[3]),ip=((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
    switch(parts[2]){
     case "LOGS":writer.WriteLine("LOGS|"+(Logs==null?"":Logs().Replace("\r","").Replace("|","/").Replace("\n","\u0001")));return;
     case "PING":writer.WriteLine("PONG|"+Environment.MachineName+"|"+(State==null?"":State()));return;
     case "RELEASE":{bool stopped=StopIfOurs!=null&&StopIfOurs();writer.WriteLine("OK|"+(stopped?"Servidor desligado no outro PC.":"Nada para desligar."));Say(who+" ("+ip+") liberou este PC"+(stopped?"; servidor desligado.":"."));return;}
     case "MAPLIST":{
      if(ip!=lastAccepted){writer.WriteLine("DENIED|Este PC não está conectado ao servidor daqui. Use Conectar ao PC servidor primeiro.");return;}
      if(parts.Length<5||WriteMapList==null){writer.WriteLine("FAILED|Rotação vazia.");return;}
      try{string previous=WriteMapList(Encoding.UTF8.GetString(Convert.FromBase64String(parts[4])));writer.WriteLine("OK|"+Convert.ToBase64String(Encoding.UTF8.GetBytes(previous)));Say("Rotação de mapas atualizada pela Central de "+who+".");}
      catch(Exception ex){writer.WriteLine("FAILED|"+Clean(ex.Message));}
      return;}
     case "HOOK":{
      if(ip!=lastAccepted){writer.WriteLine("DENIED|Este PC não está conectado ao servidor daqui. Use Conectar ao PC servidor primeiro.");return;}
      if(parts.Length<5||ApplyHook==null){writer.WriteLine("FAILED|Ajustes vazios.");return;}
      if(Interlocked.CompareExchange(ref handling,1,0)!=0){writer.WriteLine("DENIED|O outro PC está ocupado com outro pedido.");return;}
      try{
       client.ReceiveTimeout=0;bool changed=ApplyHook(Encoding.UTF8.GetString(Convert.FromBase64String(parts[4])));
       if(!changed){writer.WriteLine("OK|Os ajustes já estavam iguais no PC servidor.");return;}
       Say("Ajustes dos bots recebidos de "+who+". Reiniciando o servidor…");
       string r=RestartServer(text=>{try{writer.WriteLine("STATUS|"+Clean(text));}catch{}});
       writer.WriteLine("OK|"+Clean(r));Say("Ajustes dos bots aplicados.");
      }catch(Exception ex){try{writer.WriteLine("FAILED|"+Clean(ex.Message));}catch{}Say("Falha ao aplicar ajustes: "+ex.Message);}
      finally{Interlocked.Exchange(ref handling,0);}
      return;}
     case "CONNECT":break;     default:writer.WriteLine("FAILED|Pedido desconhecido.");return;
    }
    if(!Accepting){writer.WriteLine("DENIED|O dono do outro PC desativou os pedidos da Central.");Say("Pedido de "+who+" ("+ip+") recusado: pedidos desativados.");return;}
    if(Interlocked.CompareExchange(ref handling,1,0)!=0){writer.WriteLine("DENIED|O outro PC já está respondendo a outro pedido.");return;}
    try{
     writer.WriteLine("WAIT");Say("Pedido de "+who+" ("+ip+"). Aguardando sua resposta…");
     bool ok=Ask!=null&&Ask(who,ip);
     if(!ok){writer.WriteLine("DENIED|O dono do outro PC recusou o pedido.");Say("Você recusou o pedido de "+who+".");return;}
     writer.WriteLine("ACCEPTED");Say("Pedido de "+who+" aceito. Ligando o servidor…");lastAccepted=ip;if(Accepted!=null)try{Accepted(ip);}catch{}
     if(parts.Length>4&&ApplyHook!=null){try{if(ApplyHook(Encoding.UTF8.GetString(Convert.FromBase64String(parts[4]))))writer.WriteLine("STATUS|Ajustes dos bots da Central gravados aqui.");}catch(Exception ex){writer.WriteLine("STATUS|Ajustes dos bots não aplicados: "+Clean(ex.Message));}}
     client.ReceiveTimeout=0;
     string ready=StartServer(text=>{try{writer.WriteLine("STATUS|"+Clean(text));}catch{}});
     writer.WriteLine("READY|"+ready);Say("Servidor ligado para "+who+" ("+ip+").");
    }catch(Exception ex){try{new StreamWriter(client.GetStream()){AutoFlush=true,NewLine="\n"}.WriteLine("FAILED|"+Clean(ex.Message));}catch{}Say("Não consegui ligar para "+who+": "+ex.Message);}
    finally{Interlocked.Exchange(ref handling,0);}
   }catch(Exception ex){Say("Falha num pedido da rede: "+ex.Message);}
  }
 }
 static string Clean(string s){s=(s??"").Replace("|","/").Replace("\r"," ").Replace("\n"," ").Trim();return s.Length>80?s.Substring(0,80):s;}
 public void Dispose(){closed=true;try{if(tcp!=null)tcp.Stop();}catch{}try{if(udp!=null)udp.Close();}catch{}}
}
// Janela do pedido: fica por cima de tudo e recusa sozinha depois de 60 segundos.
public sealed class PedidoForm : Form {
 int left=60;readonly Label countdown;readonly System.Windows.Forms.Timer timer;public readonly CheckBox Always;
 public PedidoForm(string who,string ip,Icon icon){
  Text="BF4 • Pedido da Central";FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;ShowInTaskbar=true;TopMost=true;StartPosition=FormStartPosition.CenterScreen;ClientSize=new Size(500,346);BackColor=Theme.Background;Font=Theme.Body;ForeColor=Theme.Ink;if(icon!=null)Icon=icon;
  var head=new Header(null){Dock=DockStyle.Top,Height=92,Title="Pedido da BF4 Central",Subtitle="Alguém da sua rede quer usar este PC como servidor"};Controls.Add(head);
  var body=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(24,14,24,16),ColumnCount=2,RowCount=4};body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
  foreach(int h in new[]{92,34,42,52})body.RowStyles.Add(new RowStyle(SizeType.Absolute,h));
  var text=new Label{Dock=DockStyle.Fill,Text=who+" ("+ip+") quer ligar o servidor do Battlefield 4 com bots neste computador.\r\n\r\nEnquanto estiver ligado ele usa 1 núcleo do processador e até 0,6 GB de memória. Você pode desligar a qualquer momento."};
  body.Controls.Add(text,0,0);body.SetColumnSpan(text,2);
  Always=new CheckBox{Text="Sempre aceitar pedidos deste PC ("+ip+")",Dock=DockStyle.Fill};body.Controls.Add(Always,0,1);body.SetColumnSpan(Always,2);
  countdown=new Label{Dock=DockStyle.Fill,ForeColor=Theme.Muted,Font=Theme.Small};body.Controls.Add(countdown,0,2);body.SetColumnSpan(countdown,2);
  var yes=new PillButton{Text="Aceitar e ligar",BackColor=Theme.Ink,ForeColor=Color.White,Dock=DockStyle.Fill,Margin=new Padding(0,6,6,0),DialogResult=DialogResult.Yes};
  var no=new PillButton{Text="Recusar",Dock=DockStyle.Fill,Margin=new Padding(6,6,0,0),DialogResult=DialogResult.No};
  body.Controls.Add(yes,0,3);body.Controls.Add(no,1,3);Controls.Add(body);head.BringToFront();body.BringToFront();
  AcceptButton=yes;CancelButton=no;
  timer=new System.Windows.Forms.Timer{Interval=1000};timer.Tick+=(s,e)=>{left--;Tick();if(left<=0){timer.Stop();DialogResult=DialogResult.No;}};Tick();
  Shown+=(s,e)=>{timer.Start();Activate();System.Media.SystemSounds.Exclamation.Play();no.Focus();};
  FormClosed+=(s,e)=>timer.Dispose();
 }
 void Tick(){countdown.Text="Se ninguém responder, o pedido é recusado sozinho em "+left+" s e a Central volta a usar o próprio PC.";}
}
}
