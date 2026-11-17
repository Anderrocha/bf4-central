using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
// BF4 Servidor: liga e desliga, com um clique, o backend (BF4BlazeServer) e o servidor com bots
// (bf4_Server_Final) da pasta "Server" ao lado deste programa. Feito para rodar num segundo PC da
// rede de casa; o PC de jogo aponta a BF4 Central para o IP mostrado aqui.
namespace BF4Servidor {
static class Program {
 [STAThread]static int Main(string[] args){
  string root=AppDomain.CurrentDomain.BaseDirectory;
  if(args.Length>2&&args[0]=="--self-test")return SelfTest(args[1],args[2]);
  Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  bool created;using(var single=new Mutex(true,"Local\\BF4ServidorPainel",out created)){
   if(!created){MessageBox.Show("O painel do servidor já está aberto.","BF4 Servidor",MessageBoxButtons.OK,MessageBoxIcon.Information);return 0;}
   Application.Run(new Painel(root));
  }
  return 0;
 }
 static int SelfTest(string root,string output){
  var e=new Engine(root);var sb=new StringBuilder();
  sb.AppendLine("pasta Server: "+(Directory.Exists(e.ServerDir)?"ok":"FALTA"));
  sb.AppendLine("IP da rede: "+(Engine.LanIp()??"(nenhum)"));
  try{sb.AppendLine("argumentos: "+e.ServerArguments());}catch(Exception ex){sb.AppendLine("argumentos: ERRO "+ex.Message);}
  sb.AppendLine("backend aberto: "+e.Running(Engine.Backend).Count+" | servidor aberto: "+e.Running(Engine.Server).Count);
  File.WriteAllText(output,sb.ToString());return 0;
 }
}
public sealed class Engine {
 public const string Backend="BF4BlazeServer.exe",Server="bf4_Server_Final.exe";
 public readonly string Root,ServerDir;
 public Engine(string root){Root=root;ServerDir=Path.Combine(root,"Server");}
 string Exe(string name){return Path.Combine(ServerDir,name);}
 public List<Process> Running(string name){
  string expected=Path.GetFullPath(Exe(name));var list=new List<Process>();
  foreach(var p in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(name))){try{if(String.Equals(p.MainModule.FileName,expected,StringComparison.OrdinalIgnoreCase))list.Add(p);}catch{}}
  return list;
 }
 public static bool Listening(int port){try{return IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Any(e=>e.Port==port);}catch{return false;}}
 // IPv4 da placa de rede com gateway (a da rede de casa).
 public static string LanIp(){
  try{
   foreach(var nic in NetworkInterface.GetAllNetworkInterfaces().Where(n=>n.OperationalStatus==OperationalStatus.Up&&n.NetworkInterfaceType!=NetworkInterfaceType.Loopback)){
    var props=nic.GetIPProperties();if(!props.GatewayAddresses.Any(g=>g.Address.AddressFamily==AddressFamily.InterNetwork&&!g.Address.Equals(IPAddress.Any)))continue;
    var ip=props.UnicastAddresses.FirstOrDefault(u=>u.Address.AddressFamily==AddressFamily.InterNetwork);if(ip!=null)return ip.Address.ToString();
   }
  }catch{}
  return null;
 }
 static int Port(string cfg,string key,int fallback){var m=Regex.Match(cfg,"^"+key+"=(\\d+)",RegexOptions.Multiline);int v;return m.Success&&Int32.TryParse(m.Groups[1].Value,out v)?v:fallback;}
 public int ClientPort{get{return Port(File.ReadAllText(Path.Combine(ServerDir,"config.txt")),"ClientPort",10040);}}
 // Lê o !StartServer.bat original (variáveis "set" e a linha "start bf4_server_Final.exe ...").
 public string ServerArguments(){
  string bat=Path.Combine(ServerDir,"!StartServer.bat");var vars=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);string start=null;
  foreach(var raw in File.ReadAllLines(bat)){string line=raw.Trim();
   var m=Regex.Match(line,"^set\\s+\"?([A-Za-z_]+)=([^\"]*)\"?$",RegexOptions.IgnoreCase);if(m.Success){vars[m.Groups[1].Value]=m.Groups[2].Value.Replace("%~dp0",ServerDir+"\\");continue;}
   var s=Regex.Match(line,"^start\\s+(?:\"[^\"]*\"\\s+)?bf4_server_final\\.exe\\s+(.*)$",RegexOptions.IgnoreCase);if(s.Success)start=s.Groups[1].Value;
  }
  if(start==null)throw new InvalidDataException("Não achei a linha de início do servidor no !StartServer.bat.");
  string args=Regex.Replace(start,"%([A-Za-z_]+)%",x=>{string v;return vars.TryGetValue(x.Groups[1].Value,out v)?v:x.Value;});
  return args.Replace("%~dp0",ServerDir+"\\");
 }
 public int RconPort(){var m=Regex.Match(ServerArguments(),"-RemoteAdminPort\\s+(\\d+)");return m.Success?Int32.Parse(m.Groups[1].Value):25210;}
 // Grava no config.txt do backend o IP deste PC, que o backend entrega ao jogo como endereço do servidor.
 public void SetServerIp(string ip){
  string cfg=Path.Combine(ServerDir,"config.txt");string text=File.ReadAllText(cfg);
  string backup=cfg+".antes-painel";if(!File.Exists(backup))File.Copy(cfg,backup);
  string updated=Regex.IsMatch(text,"^ServerIP=.*$",RegexOptions.Multiline)?Regex.Replace(text,"^ServerIP=.*$","ServerIP="+ip,RegexOptions.Multiline):text.TrimEnd()+"\r\nServerIP="+ip+"\r\n";
  if(updated!=text)File.WriteAllText(cfg,updated);
 }
 // Regras de entrada no firewall (rede privada) para os dois programas; uma vez só.
 public void EnsureFirewall(Action<string> say){
  string marker=Path.Combine(Root,"firewall-ok.txt");if(File.Exists(marker)&&File.ReadAllText(marker).Trim()==ServerDir)return;
  say("Liberando o servidor no firewall (rede privada)…");
  foreach(var exe in new[]{Backend,Server})foreach(var proto in new[]{"TCP","UDP"}){
   string name="BF4 Servidor "+Path.GetFileNameWithoutExtension(exe)+" "+proto;
   Netsh("advfirewall firewall delete rule name=\""+name+"\"");
   int code=Netsh("advfirewall firewall add rule name=\""+name+"\" dir=in action=allow program=\""+Exe(exe)+"\" protocol="+proto+" profile=private,domain enable=yes");
   if(code!=0)throw new IOException("Não consegui criar a regra de firewall ("+name+"). Abra o painel como administrador.");
  }
  File.WriteAllText(marker,ServerDir);
 }
 static int Netsh(string args){var info=new ProcessStartInfo("netsh.exe",args){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true};using(var p=Process.Start(info)){p.StandardOutput.ReadToEnd();p.WaitForExit();return p.ExitCode;}}
 public void Start(bool light,Action<string> say){
  if(!File.Exists(Exe(Backend))||!File.Exists(Exe(Server)))throw new FileNotFoundException("A pasta Server está incompleta. Copie o pacote inteiro.");
  EnsureFirewall(say);
  string ip=LanIp();if(ip==null)throw new IOException("Este PC não está numa rede (sem IP com gateway). Conecte na mesma rede do PC de jogo.");
  SetServerIp(ip);
  if(Running(Backend).Count==0){say("Ligando o backend…");Process.Start(new ProcessStartInfo(Exe(Backend)){WorkingDirectory=ServerDir,UseShellExecute=false,CreateNoWindow=true});}
  int client=ClientPort;var sw=Stopwatch.StartNew();while(!Listening(client)){if(sw.Elapsed.TotalSeconds>40)throw new TimeoutException("O backend não abriu a porta "+client+". Veja Server\\logs.");Thread.Sleep(500);}
  int rcon=RconPort();
  if(Running(Server).Count==0){say("Ligando o servidor com bots (pode levar até 2 minutos)…");Process.Start(new ProcessStartInfo(Exe(Server),ServerArguments()){WorkingDirectory=ServerDir,UseShellExecute=true,WindowStyle=ProcessWindowStyle.Hidden});}
  sw.Restart();while(!Listening(rcon)){if(sw.Elapsed.TotalSeconds>150)throw new TimeoutException("O servidor não terminou de abrir. Veja Server\\logs.");if(Running(Server).Count==0&&sw.Elapsed.TotalSeconds>10)throw new IOException("O servidor fechou sozinho ao iniciar. Veja Server\\logs.");Thread.Sleep(1000);}
  SetPriority(light);
 }
 public void SetPriority(bool light){foreach(var p in Running(Server).Concat(Running(Backend))){try{p.PriorityClass=light?ProcessPriorityClass.BelowNormal:ProcessPriorityClass.Normal;}catch{}}}
 public void Stop(Action<string> say){
  foreach(var name in new[]{Server,Backend}){
   foreach(var p in Running(name)){say("Desligando "+(name==Server?"o servidor":"o backend")+"…");try{p.CloseMainWindow();if(!p.WaitForExit(5000)){p.Kill();p.WaitForExit(10000);}}catch{}}
  }
 }
}
public sealed class Painel : Form {
 readonly Engine engine;readonly Label state,detail,ipLabel,log;readonly Button toggle,copy;readonly CheckBox light;readonly System.Windows.Forms.Timer timer;bool busy;
 static readonly Color Ink=ColorTranslator.FromHtml("#1E3442"),Muted=ColorTranslator.FromHtml("#4D626E"),Back=ColorTranslator.FromHtml("#E8EDF0"),Green=ColorTranslator.FromHtml("#0B8A4B"),Red=ColorTranslator.FromHtml("#B3261E");
 public Painel(string root){
  engine=new Engine(root);Text="BF4 • Servidor dedicado";ClientSize=new Size(520,360);MinimumSize=new Size(480,380);StartPosition=FormStartPosition.CenterScreen;BackColor=Back;ForeColor=Ink;Font=new Font("Segoe UI",10);
  try{Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);}catch{}
  var stack=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(22,18,22,14),ColumnCount=1,RowCount=7};
  foreach(int h in new[]{40,30,74,54,32,30,0})stack.RowStyles.Add(h==0?new RowStyle(SizeType.Percent,100):new RowStyle(SizeType.Absolute,h));
  stack.Controls.Add(new Label{Text="Servidor BF4 com bots",Font=new Font("Bahnschrift",17,FontStyle.Bold),Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,0);
  state=new Label{Dock=DockStyle.Fill,Font=new Font("Segoe UI",11,FontStyle.Bold),TextAlign=ContentAlignment.MiddleLeft};stack.Controls.Add(state,0,1);
  var ipRow=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,BackColor=Color.White,Padding=new Padding(12,8,8,8),Margin=new Padding(0,4,0,4)};ipRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));ipRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,96));
  ipLabel=new Label{Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft};ipRow.Controls.Add(ipLabel,0,0);
  copy=new Button{Text="Copiar IP",Dock=DockStyle.Fill,FlatStyle=FlatStyle.System};copy.Click+=(s,e)=>{var ip=Engine.LanIp();if(ip!=null){Clipboard.SetText(ip);Say("IP copiado: "+ip);}};ipRow.Controls.Add(copy,1,0);
  stack.Controls.Add(ipRow,0,2);
  toggle=new Button{Dock=DockStyle.Fill,Font=new Font("Segoe UI",12,FontStyle.Bold),FlatStyle=FlatStyle.Flat,ForeColor=Color.White,Margin=new Padding(0,6,0,6),Cursor=Cursors.Hand};toggle.FlatAppearance.BorderSize=0;toggle.Click+=async(s,e)=>await Toggle();stack.Controls.Add(toggle,0,3);
  light=new CheckBox{Text="Modo leve: prioridade baixa (o PC continua bom para outras tarefas)",Dock=DockStyle.Fill,Checked=LoadLight()};light.CheckedChanged+=(s,e)=>{SaveLight(light.Checked);engine.SetPriority(light.Checked);};stack.Controls.Add(light,0,4);
  detail=new Label{Dock=DockStyle.Fill,ForeColor=Muted,TextAlign=ContentAlignment.MiddleLeft};stack.Controls.Add(detail,0,5);
  log=new Label{Dock=DockStyle.Fill,ForeColor=Muted,TextAlign=ContentAlignment.TopLeft};stack.Controls.Add(log,0,6);
  Controls.Add(stack);
  timer=new System.Windows.Forms.Timer{Interval=2000};timer.Tick+=(s,e)=>Refresh2();timer.Start();Refresh2();
  if(!Directory.Exists(engine.ServerDir))Say("Pasta Server não encontrada ao lado do painel. Copie o pacote inteiro.");
  FormClosing+=(s,e)=>{if(busy){e.Cancel=true;return;}if(engine.Running(Engine.Server).Count>0&&MessageBox.Show("O servidor continua ligado em segundo plano. Fechar só o painel?","BF4 Servidor",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)e.Cancel=true;};
 }
 string LightFile{get{return Path.Combine(engine.Root,"modo-leve.txt");}}
 bool LoadLight(){try{return File.Exists(LightFile)&&File.ReadAllText(LightFile).Trim()=="1";}catch{return false;}}
 void SaveLight(bool v){try{File.WriteAllText(LightFile,v?"1":"0");}catch{}}
 void Say(string text){if(InvokeRequired){BeginInvoke(new Action(()=>Say(text)));return;}log.Text=text;}
 void Refresh2(){
  if(busy)return;
  bool backend=engine.Running(Engine.Backend).Count>0,server=engine.Running(Engine.Server).Count>0,ready=server&&Engine.Listening(SafeRcon());
  string ip=Engine.LanIp()??"(sem rede)";
  ipLabel.Text="No PC de jogo, use este IP na BF4 Central:\r\n"+ip;
  if(ready){state.Text="● Ligado";state.ForeColor=Green;toggle.Text="Desligar servidor";toggle.BackColor=Red;}
  else if(backend||server){state.Text="● Ligando ou incompleto";state.ForeColor=ColorTranslator.FromHtml("#B26A00");toggle.Text="Desligar servidor";toggle.BackColor=Red;}
  else{state.Text="○ Desligado";state.ForeColor=Muted;toggle.Text="Ligar servidor";toggle.BackColor=Green;}
  detail.Text="Backend: "+(backend?"ligado":"desligado")+"   •   Servidor com bots: "+(server?(ready?"pronto":"abrindo"):"desligado");
 }
 int SafeRcon(){try{return engine.RconPort();}catch{return 25210;}}
 async Task Toggle(){
  if(busy)return;bool on=engine.Running(Engine.Server).Count>0||engine.Running(Engine.Backend).Count>0;busy=true;toggle.Enabled=false;
  toggle.Text=on?"Desligando…":"Ligando…";toggle.BackColor=Muted;
  try{
   if(on){await Task.Run(()=>engine.Stop(Say));Say("Servidor desligado. O processador deste PC está livre.");}
   else{bool l=light.Checked;await Task.Run(()=>engine.Start(l,Say));Say("Pronto. No PC de jogo, clique em Iniciar tudo e jogar.");}
  }catch(Exception ex){Say(ex.Message);}
  finally{busy=false;toggle.Enabled=true;Refresh2();}
 }
}
}
