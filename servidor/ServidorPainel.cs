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
 const string FirewallVersion="v4";
 // Quando o Windows pergunta "Permitir acesso?" e a pessoa fecha ou cancela, ele cria regras de BLOQUEIO
 // para o programa, que vencem qualquer liberação. Remove esses bloqueios do painel, backend e servidor.
 void RemoveBlocks(){
  var programs=new[]{Application.ExecutablePath,Exe(Backend),Exe(Server)};
  string list=String.Join(",",programs.Select(p=>"'"+p.Replace("'","''")+"'"));
  string script="foreach($p in @("+list+")){Get-NetFirewallApplicationFilter -Program $p -ErrorAction SilentlyContinue | Get-NetFirewallRule -ErrorAction SilentlyContinue | Where-Object {$_.Action -eq 'Block' -and $_.Direction -eq 'Inbound'} | Remove-NetFirewallRule -ErrorAction SilentlyContinue}";
  try{var info=new ProcessStartInfo("powershell.exe","-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand "+Convert.ToBase64String(Encoding.Unicode.GetBytes(script))){UseShellExecute=false,CreateNoWindow=true};using(var p=Process.Start(info))p.WaitForExit(30000);}catch{}
 }
 public void EnsureFirewall(Action<string> say){
  RemoveBlocks();
  string marker=Path.Combine(Root,"firewall-ok.txt");string mark=FirewallVersion+"|"+ServerDir+"|"+Application.ExecutablePath;if(File.Exists(marker)&&File.ReadAllText(marker).Trim()==mark)return;
  say("Liberando o servidor e os pedidos da Central no firewall (rede privada)…");
  var rules=new List<string>();
  foreach(var exe in new[]{Backend,Server})foreach(var proto in new[]{"TCP","UDP"})rules.Add("name=\"BF4 Servidor "+Path.GetFileNameWithoutExtension(exe)+" "+proto+"\" dir=in action=allow program=\""+Exe(exe)+"\" protocol="+proto);
  int rcon=25210;try{rcon=RconPort();}catch{}
  rules.Add("name=\"BF4 Servidor RCON TCP\" dir=in action=allow protocol=TCP localport="+rcon);
  rules.Add("name=\"BF4 Servidor pedidos TCP\" dir=in action=allow protocol=TCP localport=25230");
  rules.Add("name=\"BF4 Servidor controle remoto TCP\" dir=in action=allow protocol=TCP localport=25212");
  rules.Add("name=\"BF4 Servidor busca UDP\" dir=in action=allow protocol=UDP localport=25231");
  foreach(var rule in rules){
   string name=rule.Substring(0,rule.IndexOf(" dir="));
   Netsh("advfirewall firewall delete rule "+name);
   if(Netsh("advfirewall firewall add rule "+rule+" profile=private,domain enable=yes")!=0)throw new IOException("Não consegui criar a regra de firewall ("+name+"). Abra o painel como administrador.");
  }
  File.WriteAllText(marker,mark);
 }
 static int Netsh(string args){var info=new ProcessStartInfo("netsh.exe",args){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true};using(var p=Process.Start(info)){p.StandardOutput.ReadToEnd();p.WaitForExit();return p.ExitCode;}}
 // Portas que o backend precisa abrir antes do servidor com bots se registrar nele.
 List<int> BackendPorts(){
  string cfg=File.ReadAllText(Path.Combine(ServerDir,"config.txt"));var list=new List<int>{Port(cfg,"ClientPort",10040),Port(cfg,"BlazeServerPort",10041),Port(cfg,"HttpPort",80)};
  var m=Regex.Match(cfg,"^RedirectorPorts=([\\d,]+)",RegexOptions.Multiline);if(m.Success)foreach(var x in m.Groups[1].Value.Split(',')){int v;if(Int32.TryParse(x,out v))list.Add(v);}
  return list.Distinct().ToList();
 }
 // A lista de servidores do Battlelog do backend: "Nenhum servidor encontrado" = o servidor não se registrou.
 // null = não deu para saber (não bloqueia).
 public bool? Registered(){
  try{int http=Port(File.ReadAllText(Path.Combine(ServerDir,"config.txt")),"HttpPort",80);
   var req=(HttpWebRequest)WebRequest.Create("http://127.0.0.1"+(http==80?"":":"+http)+"/bf4/pt/servers/pc/");req.Proxy=null;req.Timeout=8000;req.ReadWriteTimeout=8000;
   using(var resp=req.GetResponse())using(var r=new StreamReader(resp.GetResponseStream(),Encoding.UTF8)){string page=r.ReadToEnd();if(page.Contains("Nenhum servidor encontrado"))return false;return page.Contains("data-server=")?true:(bool?)null;}
  }catch{return null;}
 }
 // Últimas linhas dos logs mais recentes (backend e servidor), para diagnóstico.
 public string LogTail(){
  var sb=new StringBuilder();
  try{foreach(var dir in new[]{Path.Combine(ServerDir,"logs"),Path.Combine(ServerDir,"Instance","Logs"),Path.Combine(ServerDir,"Instance")}){if(!Directory.Exists(dir))continue;
   foreach(var f in new DirectoryInfo(dir).GetFiles("*.*").Where(x=>x.Extension==".log"||x.Extension==".txt").OrderByDescending(x=>x.LastWriteTime).Take(2)){
    sb.AppendLine("== "+f.FullName.Substring(Root.Length)+" ("+f.LastWriteTime.ToString("HH:mm:ss")+")");
    string[] lines;using(var fs=new FileStream(f.FullName,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))using(var r=new StreamReader(fs,Encoding.UTF8))lines=r.ReadToEnd().Split('\n');
    foreach(var l in lines.Skip(Math.Max(0,lines.Length-40)))sb.AppendLine(l.TrimEnd());
   }}}catch(Exception ex){sb.AppendLine("erro: "+ex.Message);}
  sb.AppendLine("lista do jogo: "+(Registered()==false?"vazia":"ok/desconhecida")+" | backend "+Running(Backend).Count+" | servidor "+Running(Server).Count);
  string t=sb.ToString();return t.Length>60000?t.Substring(t.Length-60000):t;
 }
 void LaunchServer(Action<string> say){say("Ligando o servidor com bots (pode levar até 2 minutos)…");Process.Start(new ProcessStartInfo(Exe(Server),ServerArguments()){WorkingDirectory=ServerDir,UseShellExecute=true,WindowStyle=ProcessWindowStyle.Hidden});}
 void WaitRcon(int rcon){var sw=Stopwatch.StartNew();while(!Listening(rcon)){if(sw.Elapsed.TotalSeconds>150)throw new TimeoutException("O servidor não terminou de abrir. Veja Server\\logs.");if(Running(Server).Count==0&&sw.Elapsed.TotalSeconds>10)throw new IOException("O servidor fechou sozinho ao iniciar. Veja Server\\logs.");Thread.Sleep(1000);}}
 bool WaitRegistered(int seconds){var sw=Stopwatch.StartNew();while(sw.Elapsed.TotalSeconds<seconds){var r=Registered();if(r!=false)return true;Thread.Sleep(3000);}return false;}
 void StopServerOnly(){foreach(var p in Running(Server)){try{p.CloseMainWindow();if(!p.WaitForExit(5000)){p.Kill();p.WaitForExit(10000);}}catch{}}}
 public void Start(bool light,Action<string> say){
  if(!File.Exists(Exe(Backend))||!File.Exists(Exe(Server)))throw new FileNotFoundException("A pasta Server está incompleta. Copie o pacote inteiro.");
  // Sem o mod (dinput8.dll) o servidor abre, mas sem bots e sem se registrar no backend: some da lista do jogo.
  var hook=new FileInfo(Exe("dinput8.dll"));
  if(!hook.Exists||hook.Length<1000000)throw new FileNotFoundException("Falta o arquivo Server\\dinput8.dll (o mod dos bots). O antivírus deste PC provavelmente o removeu. Sem ele o servidor não aparece na lista. Veja Segurança do Windows → Histórico de proteção.");
  EnsureFirewall(say);
  string ip=LanIp();if(ip==null)throw new IOException("Este PC não está numa rede (sem IP com gateway). Conecte na mesma rede do PC de jogo.");
  SetServerIp(ip);
  bool fresh=false;
  if(Running(Backend).Count==0){say("Ligando o backend…");Process.Start(new ProcessStartInfo(Exe(Backend)){WorkingDirectory=ServerDir,UseShellExecute=false,CreateNoWindow=true});fresh=true;}
  var sw=Stopwatch.StartNew();
  foreach(int port in BackendPorts())while(!Listening(port)){if(sw.Elapsed.TotalSeconds>60)throw new TimeoutException("O backend não abriu a porta "+port+". Veja Server\\logs.");Thread.Sleep(500);}
  if(fresh)Thread.Sleep(4000); // deixa o backend terminar de carregar antes do servidor se registrar
  int rcon=RconPort();
  if(Running(Server).Count==0)LaunchServer(say);
  WaitRcon(rcon);
  say("Conferindo se o servidor aparece na lista do jogo…");
  if(!WaitRegistered(75)){
   say("O servidor não apareceu na lista; reiniciando só o servidor…");
   StopServerOnly();Thread.Sleep(3000);LaunchServer(say);WaitRcon(rcon);
   if(!WaitRegistered(90))throw new IOException("O servidor ligou, mas não aparece na lista do jogo. Desligue e ligue de novo; se continuar, veja Server\\logs.");
  }
  SetPriority(light);
 }
 // Ajustes dos bots vindos da Central: só troca o valor de chaves que já existem no ini deste PC,
 // com valores simples (números, letras, ; . - _ espaço). Comentários e o resto do arquivo ficam como estão.
 public bool ApplyHook(string incoming){
  string path=Exe("dinput8_bf4hook.ini");string text;using(var r=new StreamReader(path,Encoding.UTF8,true))text=r.ReadToEnd();
  var values=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);bool section=false;
  foreach(var raw in incoming.Split('\n')){string t=raw.Trim();if(t.StartsWith("[")){section=String.Equals(t,"[bf4hook]",StringComparison.OrdinalIgnoreCase);continue;}
   if(!section||t.Length==0||t.StartsWith(";")||t.StartsWith("#"))continue;var m=Regex.Match(t,"^([A-Za-z0-9_]{1,60})\\s*=\\s*([A-Za-z0-9_.;\\- ]{0,200})$");if(m.Success){string k=m.Groups[1].Value,v=m.Groups[2].Value.Trim();if(k!="ChatCommandAdmins"&&v.IndexOfAny(new[]{';',' '})>=0)continue;values[k]=v;}}
  if(values.Count==0)throw new InvalidDataException("Ajustes dos bots vazios ou inválidos.");
  bool changed=false;section=false;var lines=Regex.Split(text,"(?<=\\n)");
  for(int i=0;i<lines.Length;i++){string t=lines[i].Trim();if(t.StartsWith("[")){section=String.Equals(t,"[bf4hook]",StringComparison.OrdinalIgnoreCase);continue;}
   if(!section)continue;var m=Regex.Match(lines[i],"^(\\s*)([A-Za-z0-9_]+)(\\s*=\\s*)([^\\r\\n;#]*?)(\\s*[;#][^\\r\\n]*)?(\\r?\\n)?$");string v;
   if(m.Success&&values.TryGetValue(m.Groups[2].Value,out v)&&m.Groups[4].Value.Trim()!=v){lines[i]=m.Groups[1].Value+m.Groups[2].Value+m.Groups[3].Value+v+m.Groups[5].Value+m.Groups[6].Value;changed=true;}}
  if(!changed)return false;
  string backup=path+".antes-central";if(!File.Exists(backup))File.Copy(path,backup);
  string tmp=path+".tmp";File.WriteAllText(tmp,String.Concat(lines),new UTF8Encoding(false));File.Copy(tmp,path,true);File.Delete(tmp);
  return true;
 }
 // Rotação vinda da Central: linhas "Mapa Modo Rodadas" (só letras, números e _), até 1000. Backup na primeira vez.
 public string WriteMapList(string content){
  var lines=content.Replace("\r","").Split(new[]{'\n'},StringSplitOptions.RemoveEmptyEntries).Select(l=>l.Trim()).Where(l=>l.Length>0).ToList();
  if(lines.Count<1||lines.Count>1000||lines.Any(l=>!Regex.IsMatch(l,"^[A-Za-z0-9_]{1,40} [A-Za-z0-9_]{1,40} [1-9][0-9]?$")))throw new InvalidDataException("Rotação inválida.");
  string file=Path.Combine(ServerDir,"Instance","Admin","MapList.txt");string previous=File.Exists(file)?File.ReadAllText(file):"";
  string backup=file+".antes-central";if(File.Exists(file)&&!File.Exists(backup))File.Copy(file,backup);
  string tmp=file+".tmp";File.WriteAllText(tmp,String.Join("\r\n",lines)+"\r\n",new UTF8Encoding(false));File.Copy(tmp,file,true);File.Delete(tmp);
  return previous;
 }
 // Reinicia só o servidor com bots (o backend continua) e confere se voltou para a lista.
 public void RestartServerOnly(bool light,Action<string> say){
  if(Running(Server).Count==0)return;
  say("Reiniciando o servidor para valer os ajustes dos bots…");StopServerOnly();Thread.Sleep(3000);LaunchServer(say);WaitRcon(RconPort());
  say("Conferindo se o servidor aparece na lista do jogo…");
  if(!WaitRegistered(75)){say("O servidor não apareceu na lista; reiniciando de novo…");StopServerOnly();Thread.Sleep(3000);LaunchServer(say);WaitRcon(RconPort());if(!WaitRegistered(90))throw new IOException("O servidor reiniciou, mas não aparece na lista do jogo.");}
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
