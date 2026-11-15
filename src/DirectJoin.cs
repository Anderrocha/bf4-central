using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
namespace BF4Bots {
// Entrada direta no servidor local (Battlelog do próprio backend), detecção de conexão
// por qualquer endereço desta máquina e garantia de tela inteira.
public static partial class OneClick {
 [DllImport("user32.dll")]static extern int GetWindowLong(IntPtr window,int index);
 [DllImport("user32.dll")]static extern bool IsIconic(IntPtr window);
 [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr window,out Rect rect);
 [DllImport("user32.dll")]static extern void keybd_event(byte key,byte scan,uint flags,UIntPtr extra);
 [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
 [StructLayout(LayoutKind.Sequential)]struct Rect{public int Left,Top,Right,Bottom;}

 // ---------- Conexão: o backend entrega ao jogo o ServerIP (ex.: 192.168.0.8), não 127.0.0.1 ----------
 static HashSet<IPAddress> ownAddresses;static DateTime ownAddressesAt;
 public static bool IsOwnAddress(IPAddress address){
  if(IPAddress.IsLoopback(address))return true;
  if(ownAddresses==null||(DateTime.UtcNow-ownAddressesAt).TotalSeconds>30){
   var set=new HashSet<IPAddress>();
   try{foreach(var nic in NetworkInterface.GetAllNetworkInterfaces())foreach(var u in nic.GetIPProperties().UnicastAddresses)if(u.Address.AddressFamily==AddressFamily.InterNetwork)set.Add(u.Address);}catch{}
   ownAddresses=set;ownAddressesAt=DateTime.UtcNow;
  }
  return ownAddresses.Contains(address);
 }
 static bool BackendAddress(IPAddress remote,string host){
  IPAddress expected;if(!IPAddress.TryParse(host,out expected))return false;
  if(remote.Equals(expected))return true;
  // Backend local: o redirecionador pode indicar qualquer IP deste PC.
  return IPAddress.IsLoopback(expected)&&IsOwnAddress(remote);
 }

 // ---------- Preferência: entrar direto no servidor ou abrir a tela inicial do BF4 ----------
 static string DirectJoinFile{get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BF4BotLauncher","direct-join.txt");}}
 public static bool DirectJoinEnabled{
  get{try{return !File.Exists(DirectJoinFile)||File.ReadAllText(DirectJoinFile).Trim()!="0";}catch{return true;}}
  set{try{Directory.CreateDirectory(Path.GetDirectoryName(DirectJoinFile));File.WriteAllText(DirectJoinFile,value?"1":"0");}catch(IOException){}catch(UnauthorizedAccessException){}}
 }

 // ---------- Núcleos separados: jogo × servidor/bots ----------
 static string SplitCoresFile{get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BF4BotLauncher","split-cores.txt");}}
 public static bool SplitCoresEnabled{
  get{try{return !File.Exists(SplitCoresFile)||File.ReadAllText(SplitCoresFile).Trim()!="0";}catch{return true;}}
  set{try{Directory.CreateDirectory(Path.GetDirectoryName(SplitCoresFile));File.WriteAllText(SplitCoresFile,value?"1":"0");}catch(IOException){}catch(UnauthorizedAccessException){}}
 }
 // Com n núcleos: o jogo usa todos menos o último; o servidor (IA dos bots) usa os dois últimos; o backend, só o último.
 // 4 núcleos → jogo 1-3, servidor 3-4, backend 4. O núcleo 3 é dividido, com prioridade para o jogo.
 public static long[] CoreMasks(int n){
  if(n<4)return null;long all=(1L<<n)-1,last=1L<<(n-1),beforeLast=1L<<(n-2);
  return new[]{all&~last,last|beforeLast,last,all};
 }
 public static string ApplyCores(string root,Process game){
  var masks=CoreMasks(Math.Min(Environment.ProcessorCount,62));if(masks==null)return null;bool on=SplitCoresEnabled;
  Action<Process,long,ProcessPriorityClass> set=(p,mask,priority)=>{try{p.Refresh();if(p.HasExited)return;p.ProcessorAffinity=(IntPtr)mask;p.PriorityClass=priority;}catch{}};
  set(game,on?masks[0]:masks[3],on?ProcessPriorityClass.AboveNormal:ProcessPriorityClass.Normal);
  foreach(var s in At(Path.Combine(root,"Server\\bf4_Server_Final.exe")))set(s,on?masks[1]:masks[3],ProcessPriorityClass.Normal);
  foreach(var b in At(Path.Combine(root,"Server\\BF4BlazeServer.exe")))set(b,on?masks[2]:masks[3],on?ProcessPriorityClass.BelowNormal:ProcessPriorityClass.Normal);
  return on?"Núcleos separados: jogo com prioridade; servidor e bots nos últimos núcleos.":null;
 }

 // ---------- Entrada direta pelo Battlelog local ----------
 class LocalWeb {
  readonly CookieContainer cookies=new CookieContainer();readonly string origin;
  public LocalWeb(int port){origin="http://127.0.0.1"+(port==80?"":":"+port);}
  public string Send(string path,string method,string form,out int status){
   var uri=new Uri(origin+path);if(uri.Host!="127.0.0.1"||uri.Scheme!="http")throw new InvalidOperationException("O Battlelog local aceita somente 127.0.0.1.");
   var req=(HttpWebRequest)WebRequest.Create(uri);req.Proxy=null;req.Method=method;req.Timeout=8000;req.ReadWriteTimeout=8000;req.AllowAutoRedirect=true;req.CookieContainer=cookies;req.Accept="application/json, text/html";
   if(form!=null){var bytes=Encoding.UTF8.GetBytes(form);req.ContentType="application/x-www-form-urlencoded";req.ContentLength=bytes.Length;using(var s=req.GetRequestStream())s.Write(bytes,0,bytes.Length);}
   HttpWebResponse response;try{response=(HttpWebResponse)req.GetResponse();}catch(WebException ex){if(ex.Response==null)throw;response=(HttpWebResponse)ex.Response;}
   using(response)using(var r=new StreamReader(response.GetResponseStream(),Encoding.UTF8)){status=(int)response.StatusCode;string text=r.ReadToEnd();if(text.Length>4*1024*1024)throw new IOException("Resposta local excessiva.");return text;}
  }
 }
 static string Form(params string[] pairs){var sb=new StringBuilder();for(int i=0;i+1<pairs.Length;i+=2){if(sb.Length>0)sb.Append('&');sb.Append(Uri.EscapeDataString(pairs[i])).Append('=').Append(Uri.EscapeDataString(pairs[i+1]??""));}return sb.ToString();}
 public static string FirstServer(string page){
  var m=Regex.Match(page,"data-game-launch=\"multiplayer\"\\s+data-server=\"([^\"]+)\"");return m.Success?WebUtility.HtmlDecode(m.Groups[1].Value):null;
 }
 static Dictionary<string,object> LauncherConfig(string page){
  var m=Regex.Match(page,"<script id=\"battlelog-launcher-config\" type=\"application/json\">([\\s\\S]*?)</script>");
  if(!m.Success)return null;try{return Json.Deserialize<Dictionary<string,object>>(m.Groups[1].Value);}catch{return null;}
 }
 static string Str(Dictionary<string,object> d,string key){object v;return d!=null&&d.TryGetValue(key,out v)&&v!=null?Convert.ToString(v):"";}
 // Devolve a linha de comando que entra direto no servidor local, ou null para usar o início normal.
 public static string DirectJoinCommand(string root,string cfg,Action<string> progress,out Action release){
  release=null;
  try{
   string backendCfg=File.ReadAllText(Path.Combine(root,"Server\\config.txt"));int http=Port(backendCfg,"HttpPort",80);
   string user=Value(cfg,"UserId","");if(user.Length==0)return null;
   var web=new LocalWeb(http);int status;
   progress("Preparando o launcher: entrando no Battlelog local…");
   web.Send("/bf4/en/login/","POST",Form("username",user,"password",""),out status);
   string server=null;Dictionary<string,object> config=null;var watch=Stopwatch.StartNew();
   progress("Preparando o launcher: aguardando o servidor aparecer na lista…");
   while(watch.Elapsed.TotalSeconds<90){
    string page=web.Send("/bf4/en/servers/pc","GET",null,out status);
    config=LauncherConfig(page);if(config==null||Str(config,"playerId").Length==0)return null; // login não aceito (ex.: senha)
    server=FirstServer(page);if(server!=null)break;Thread.Sleep(2000);
   }
   if(server==null)return null;
   string request=Guid.NewGuid().ToString("N");string prepare=Str(config,"prepareUrl");if(!prepare.StartsWith("/"))return null;
   string reply=web.Send(prepare,"POST",Form("post-check-sum",Str(config,"csrf"),"mode","multiplayer","server",server,"role","soldier","password","","requestId",request),out status);
   var plan=Json.Deserialize<Dictionary<string,object>>(reply);
   if(status>=400||plan==null||!(plan.ContainsKey("success")&&plan["success"] is bool&&(bool)plan["success"]))return null;
   string command=Str(plan,"commandLine");if(command.Length==0||Str(plan,"requestId")!=request||command.IndexOfAny(new[]{'\r','\n','\0'})>=0)return null;
   string cancel=Str(config,"cancelUrl"),csrf=Str(config,"csrf");
   release=()=>{try{int s;web.Send(cancel,"POST",Form("post-check-sum",csrf,"requestId",request),out s);}catch{}};
   string extra=Value(cfg,"ExtraArguments","");if(!String.IsNullOrWhiteSpace(extra)){GameCommand("127.0.0.1",extra);command+=" "+extra;}
   return command;
  }catch(Exception ex){if(ex is InvalidDataException)throw;return null;}
 }

 // ---------- Tela inteira ----------
 public static bool WantsFullscreen(){
  try{string profile=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"Battlefield 4\\settings\\PROFSAVE_profile");if(!File.Exists(profile))return true;
   var line=File.ReadAllLines(profile).FirstOrDefault(l=>l.StartsWith("GstRender.FullscreenEnabled ",StringComparison.Ordinal));return line==null||line.Trim().EndsWith(" 1");}catch{return true;}
 }
 public static bool IsWindowed(IntPtr window){
  const int GWL_STYLE=-16;const int WS_CAPTION=0x00C00000;
  if(window==IntPtr.Zero||IsIconic(window))return false;
  if((GetWindowLong(window,GWL_STYLE)&WS_CAPTION)!=WS_CAPTION)return false;
  Rect r;if(!GetWindowRect(window,out r))return false;
  var screen=System.Windows.Forms.Screen.FromHandle(window).Bounds;
  return r.Right-r.Left<screen.Width||r.Bottom-r.Top<screen.Height;
 }
 // Se o jogo abriu em janela e o perfil pede tela inteira, alterna com Alt+Enter (atalho nativo do BF4).
 public static void EnsureFullscreen(Process game){
  if(!WantsFullscreen())return;
  for(int attempt=0;attempt<2;attempt++){
   game.Refresh();if(game.HasExited)return;IntPtr h=game.MainWindowHandle;
   if(!IsWindowed(h))return;
   SetForegroundWindow(h);Thread.Sleep(400);if(GetForegroundWindow()!=h)return;
   const byte ALT=0x12,ENTER=0x0D;const uint UP=0x2;
   keybd_event(ALT,0,0,UIntPtr.Zero);keybd_event(ENTER,0,0,UIntPtr.Zero);keybd_event(ENTER,0,UP,UIntPtr.Zero);keybd_event(ALT,0,UP,UIntPtr.Zero);
   Thread.Sleep(2500);
  }
 }
}
}
