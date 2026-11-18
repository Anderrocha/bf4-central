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
   if(!created){if(!args.Contains("--tray"))MessageBox.Show("O painel do servidor já está aberto (veja o ícone perto do relógio).","BF4 Servidor",MessageBoxButtons.OK,MessageBoxIcon.Information);return 0;}
   Application.Run(new Painel(root,args.Contains("--tray")));
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
 // Regras de entrada no firewall (rede privada), refeitas quando a versão das regras muda:
 // programas do backend e do servidor, porta RCON (o controle da Central) e o próprio painel (pedidos).
 const string FirewallVersion="v3";
 public void EnsureFirewall(Action<string> say){
  string marker=Path.Combine(Root,"firewall-ok.txt");string mark=FirewallVersion+"|"+ServerDir+"|"+Application.ExecutablePath;if(File.Exists(marker)&&File.ReadAllText(marker).Trim()==mark)return;
  say("Liberando o servidor e os pedidos da Central no firewall (rede privada)…");
  var rules=new List<string>();
  foreach(var exe in new[]{Backend,Server})foreach(var proto in new[]{"TCP","UDP"})rules.Add("name=\"BF4 Servidor "+Path.GetFileNameWithoutExtension(exe)+" "+proto+"\" dir=in action=allow program=\""+Exe(exe)+"\" protocol="+proto);
  int rcon=25210;try{rcon=RconPort();}catch{}
  rules.Add("name=\"BF4 Servidor RCON TCP\" dir=in action=allow protocol=TCP localport="+rcon);
  rules.Add("name=\"BF4 Servidor pedidos TCP\" dir=in action=allow program=\""+Application.ExecutablePath+"\" protocol=TCP localport=25230");
  rules.Add("name=\"BF4 Servidor controle remoto TCP\" dir=in action=allow program=\""+Application.ExecutablePath+"\" protocol=TCP localport=25212");
  rules.Add("name=\"BF4 Servidor busca UDP\" dir=in action=allow program=\""+Application.ExecutablePath+"\" protocol=UDP localport=25231");
  foreach(var rule in rules){
   string name=rule.Substring(0,rule.IndexOf(" dir="));
   Netsh("advfirewall firewall delete rule "+name);
   if(Netsh("advfirewall firewall add rule "+rule+" profile=private,domain enable=yes")!=0)throw new IOException("Não consegui criar a regra de firewall ("+name+"). Abra o painel como administrador.");
  }
  File.WriteAllText(marker,mark);
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
}
