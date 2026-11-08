using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
namespace BF4Bots {
public static class Performance {
 public static Dictionary<string,string> LightValues(string text) {
  var v=new Dictionary<string,string>{{"BotMatchChat","0"},{"BotCasualChat","0"},{"BotRoundChat","0"},{"BotScoreboardPing","0"},{"BotRandomRank","0"},{"BotInfiniteGrenades","0"}};
  foreach(var kv in new Dictionary<string,int>{{"BotGrenadeCooldownMinMs",8000},{"BotGrenadeCooldownMaxMs",12000},{"BotGrenadeTeamGapMs",1500},{"BotSupplyCooldownMs",5000}}){int value;if(!Int32.TryParse(Config.Read(text,kv.Key),out value))throw new InvalidDataException("Valor inválido: "+kv.Key);v[kv.Key]=Math.Max(value,kv.Value).ToString();}return v;
 }
 public static string Set(string text,string key,string value,string separator) {
  string pattern=@"(?im)^(\s*"+Regex.Escape(key)+@"\s*"+Regex.Escape(separator)+@"\s*)([^\r\n;#]*)([^\r\n]*)(\r?)$";
  if(Regex.Matches(text,pattern).Count!=1)throw new InvalidDataException("Parâmetro ausente ou duplicado: "+key);
  return Regex.Replace(text,pattern,m=>m.Groups[1].Value+value+m.Groups[3].Value+m.Groups[4].Value);
 }
 public static string Light(string text){foreach(var kv in LightValues(text))text=Set(text,kv.Key,kv.Value,"=");return text;}
 public static Dictionary<string,byte[]> Plan(string root){
  Config.ValidateRoot(root);var changes=new Dictionary<string,byte[]>();
  string hook=Path.Combine(root,"Server\\dinput8_bf4hook.ini"),backend=Path.Combine(root,"Server\\config.txt"),engine=Path.Combine(root,"Server\\Scripts\\Win32Game.cfg"),startup=Path.Combine(root,"Server\\Instance\\Admin\\Startup.txt"),cfg=Path.Combine(root,"user.cfg");
  changes[hook]=new UTF8Encoding(false).GetBytes(Light(File.ReadAllText(hook)));
  string t=File.ReadAllText(backend);foreach(string key in new[]{"TraceRpc","TraceHttp","LogConsoleEnabled"})t=Set(t,key,"0","=");changes[backend]=new UTF8Encoding(false).GetBytes(t);
  t=File.ReadAllText(engine);foreach(string key in new[]{"-ServerMetrics.TransactionTelemetryEnabled","-ServerMetrics.PerformanceTelemetryEnabled","-ServerMetrics.PerformanceProfileStateEnabled","-TransactionLog.OutputLevel"})t=Set(t,key,"0"," ");changes[engine]=new UTF8Encoding(false).GetBytes(t);
  t=File.ReadAllText(startup);t=Set(t,"vars.OutHighFrequency","30"," ");changes[startup]=new UTF8Encoding(false).GetBytes(t);
  t=File.Exists(cfg)?File.ReadAllText(cfg):"";string fps=@"(?im)^\s*GameTime\.MaxVariableFps\s+[^\r\n]*";int count=Regex.Matches(t,fps).Count;if(count>1)throw new InvalidDataException("Limite de FPS duplicado em user.cfg.");t=count==1?Regex.Replace(t,fps,"GameTime.MaxVariableFps 60"):t.TrimEnd()+"\r\nGameTime.MaxVariableFps 60\r\n";changes[cfg]=new UTF8Encoding(false).GetBytes(t);
  return changes;
 }
 public static void Test(string root,string report){var plan=Plan(root);string ini=Encoding.UTF8.GetString(plan[Path.Combine(root,"Server\\dinput8_bf4hook.ini")]);if(Config.Read(ini,"BotMatchChat")!="0"||Config.Read(ini,"BotInfiniteGrenades")!="0")throw new Exception("Modo leve inválido");if(Light(ini)!=ini)throw new Exception("Modo leve não idempotente");foreach(string key in new[]{"EnableAim","EnableFire","ChatCommands","VehicleBots","ObjectiveShare"})if(Config.Read(ini,key)!=Config.Read(File.ReadAllText(Path.Combine(root,"Server\\dinput8_bf4hook.ini")),key))throw new Exception("Opção indevida alterada: "+key);
  File.WriteAllText(report,"PASS plano de cinco arquivos\r\nPASS modo leve idempotente\r\nPASS preserva mira, tiros, comandos, veículos e objetivos\r\nPASS nenhuma gravação na instalação real\r\n"+String.Join("\r\n",plan.Keys),Encoding.UTF8);
 }
 public static string Apply(string root){
  var principal=new WindowsPrincipal(WindowsIdentity.GetCurrent());if(!principal.IsInRole(WindowsBuiltInRole.Administrator))throw new UnauthorizedAccessException("Execute a atualização como administrador.");
  var plan=Plan(root);var servers=Server.Find(root,"bf4_Server_Final.exe").OrderByDescending(p=>p.WorkingSet64).ToList();var backends=Server.Find(root,"BF4BlazeServer.exe");string serverArgs=null;
  if(servers.Count>0)using(var q=new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId="+servers[0].Id))foreach(ManagementObject o in q.Get()){var m=Regex.Match((string)o["CommandLine"]??"",@"^\s*(?:""[^""]+""|\S+)\s*(.*)$",RegexOptions.Singleline);if(m.Success)serverArgs=m.Groups[1].Value;}
  if(servers.Count>0&&String.IsNullOrWhiteSpace(serverArgs))throw new InvalidOperationException("Não consegui preservar os argumentos do servidor. Nada foi alterado.");
  string backup=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BF4BotLauncher","performance-backups",DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,6));Directory.CreateDirectory(backup);var originals=new Dictionary<string,byte[]>();
  foreach(var kv in plan){byte[] original=File.Exists(kv.Key)?File.ReadAllBytes(kv.Key):null;originals[kv.Key]=original;string relative=kv.Key.Substring(root.TrimEnd('\\').Length).TrimStart('\\');string dest=Path.Combine(backup,relative);Directory.CreateDirectory(Path.GetDirectoryName(dest));if(original!=null)File.WriteAllBytes(dest,original);else File.WriteAllText(dest+".was-absent","Arquivo não existia.");}
  File.WriteAllText(Path.Combine(backup,"README.txt"),"Backup anterior à otimização. Servidor e backend foram reiniciados para ativar os ajustes. user.cfg vale ao reabrir o jogo. Conexão e senhas foram preservadas.\r\n");
  var committed=new List<string>();try{foreach(var kv in plan){if(originals[kv.Key]!=null&&Config.Hash(File.ReadAllBytes(kv.Key))!=Config.Hash(originals[kv.Key]))throw new IOException("Arquivo modificado durante a operação: "+kv.Key);string temp=kv.Key+".performance-"+Guid.NewGuid().ToString("N")+".tmp";File.WriteAllBytes(temp,kv.Value);try{if(File.Exists(kv.Key))File.Replace(temp,kv.Key,null);else File.Move(temp,kv.Key);}finally{if(File.Exists(temp))File.Delete(temp);}committed.Add(kv.Key);if(Config.Hash(File.ReadAllBytes(kv.Key))!=Config.Hash(kv.Value))throw new IOException("Falha na verificação: "+kv.Key);}}
  catch{foreach(string p in committed){if(originals[p]!=null)File.WriteAllBytes(p,originals[p]);else if(File.Exists(p))File.Delete(p);}throw;}
  string result="Configurações aplicadas e verificadas. Backup: "+backup+"\r\n";
  try{
   foreach(var p in servers.Concat(backends)){string expected=Path.GetFullPath(Path.Combine(root,"Server",p.ProcessName+".exe"));if(!String.Equals(p.MainModule.FileName,expected,StringComparison.OrdinalIgnoreCase))throw new Exception("Processo mudou; reinício interrompido.");p.Kill();if(!p.WaitForExit(10000))throw new Exception("Não foi possível encerrar "+p.ProcessName);}
   if(serverArgs!=null){Process.Start(new ProcessStartInfo(Path.Combine(root,"Server\\BF4BlazeServer.exe")){WorkingDirectory=Path.Combine(root,"Server"),UseShellExecute=true,WindowStyle=ProcessWindowStyle.Hidden});System.Threading.Thread.Sleep(2500);Process.Start(new ProcessStartInfo(Path.Combine(root,"Server\\bf4_Server_Final.exe"),serverArgs){WorkingDirectory=Path.Combine(root,"Server"),UseShellExecute=true,WindowStyle=ProcessWindowStyle.Hidden});}else Server.Start(root,null);
   result+="Reinício do servidor solicitado. Processos de servidor anteriores: "+servers.Count+". Backend anterior: "+backends.Count+".\r\n";
  }catch(Exception ex){result+="Os arquivos foram atualizados, mas o reinício falhou: "+ex.Message+". Reinicie o backend e o servidor manualmente.\r\n";}
  result+="Reabra o jogo para ativar o limite de 60 FPS e reconecte. Comece com !setbot 20.\r\n";return result;
 }
}
public static class OptimizeProgram {
 [STAThread] public static int Main(string[] args){string root="C:\\Program Files\\EA Games\\Battlefield 4";string output=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"tests","performance-applied.txt");try{string result=Performance.Apply(root);File.WriteAllText(output,result,Encoding.UTF8);MessageBox.Show(result,"BF4 — atualização de fluidez",MessageBoxButtons.OK,MessageBoxIcon.Information);return 0;}catch(Exception ex){File.WriteAllText(output,"FALHA: "+ex.Message,Encoding.UTF8);MessageBox.Show(ex.Message,"BF4 — atualização não concluída",MessageBoxButtons.OK,MessageBoxIcon.Error);return 1;}}
}
}
