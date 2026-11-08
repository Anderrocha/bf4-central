using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BF4Bots {
public sealed class Profile {
 public string Name, Summary, Detail;
 public Dictionary<string,string> Values = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
 public Profile(string name,string summary,string detail,string[] values) {
  Name=name; Summary=summary; Detail=detail;
  string[] keys={"ObjectiveShare","RoamerDepthWeight","BotFrontlineSpawn","BotSquadSpawn","BotMedicRevive","BotHotspotSupplies","BotLauncherSupport","BotClusterGrenades","BotGrenadeClearObjectives","BotInfiniteGrenades","BotGrenadeCooldownMinMs","BotGrenadeCooldownMaxMs","BotGrenadeTeamGapMs","VehicleBots"};
  if(keys.Length!=values.Length) throw new InvalidDataException("Perfil inválido.");
  for(int i=0;i<keys.Length;i++) Values.Add(keys[i],values[i]);
 }
 public static readonly Profile[] All={
  new Profile("Noob","Pouco apoio e pressão sobre objetivos.","Exploração mais livre; sem renascimento na linha de frente ou no esquadrão. Sem reanimação, apoio de munição, veículos ou rotina de granadas do mod. Os bots continuam usando a mira e os tiros padrão do mod.",new[]{"0.20","0.20","0","0","0","0","0","0","0","0","15000","20000","3000","0"}),
  new Profile("Fácil","Apoio básico e objetivos mais ativos.","Maior presença nos objetivos. Apoio de munição e reanimação ativos; sem renascimento de esquadrão, veículos ou rotina de granadas do mod. Mira e tiros permanecem no padrão do mod.",new[]{"0.40","0.35","0","0","1","1","0","0","0","0","12000","18000","2500","0"}),
  new Profile("Normal","Esquadrões ativos e pressão equilibrada.","Renascimento no esquadrão, reanimação, suprimentos e veículos. Rotina de granadas ativa, com intervalo configurado de 8–12 s e sem reserva infinita. Mira e tiros permanecem no padrão do mod.",new[]{"0.60","0.50","0","1","1","1","1","1","0","0","8000","12000","1500","1"}),
  new Profile("Difícil","Avanço mais próximo e apoio completo.","Renascimento na linha de frente e no esquadrão. Captura de objetivos mais concentrada, veículos e limpeza de objetivos com granadas. Intervalo configurado das granadas: 5–8 s. Mira e tiros permanecem no padrão do mod.",new[]{"0.80","0.65","1","1","1","1","1","1","1","0","5000","8000","1000","1"}),
  new Profile("Extremo","Máxima pressão dos controles disponíveis.","Mais bots destinados aos objetivos, apoio completo, veículos e rotina de granadas com reserva infinita e intervalo de 3–6 s. Pode acrescentar carga ao servidor. Mira e tiros permanecem no padrão do mod.",new[]{"0.95","0.85","1","1","1","1","1","1","1","1","3000","6000","500","1"})
 };
}
public static class Config {
 public const string DllHash="b96f7bd37a4b62090e13c3a13cda5b2b213deabfc16f4c49c8c5da7d535e0428";
 public static string Hash(byte[] b) { using(var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(b)).Replace("-","").ToLowerInvariant(); }
 public static string Read(string text,string key) {
  bool section=false; string value=null;
  foreach(string line in Regex.Split(text,"\\r?\\n")) {
   string t=line.Trim(); if(t.StartsWith("[")){section=String.Equals(t,"[bf4hook]",StringComparison.OrdinalIgnoreCase);continue;}
   if(!section||t.StartsWith(";")||t.StartsWith("#")) continue;
   var m=Regex.Match(line,"^\\s*"+Regex.Escape(key)+"\\s*=\\s*([^;#]*)(?:[;#].*)?$");
   if(m.Success){ if(value!=null) throw new InvalidDataException("Chave duplicada: "+key); value=m.Groups[1].Value.Trim(); }
  }
  return value;
 }
 public static string Render(string text,Profile p) {
  if(Regex.Matches(text,@"(?im)^\s*\[bf4hook\]\s*$").Count!=1) throw new InvalidDataException("A configuração deve conter uma seção [bf4hook].");
  foreach(var kv in p.Values) if(Read(text,kv.Key)==null) throw new InvalidDataException("Parâmetro ausente: "+kv.Key+". Esta instalação precisa de revisão.");
  bool section=false; var lines=Regex.Split(text,"(?<=\\n)");
  for(int i=0;i<lines.Length;i++) {
   string t=lines[i].Trim(); if(t.StartsWith("[")){section=String.Equals(t,"[bf4hook]",StringComparison.OrdinalIgnoreCase);continue;}
   if(!section)continue;
   var m=Regex.Match(lines[i],@"^(\s*)([A-Za-z0-9_]+)(\s*=\s*)([^\r\n;#]*)([^\r\n]*)(\r?\n)?$");
   string v; if(m.Success&&p.Values.TryGetValue(m.Groups[2].Value,out v)) lines[i]=m.Groups[1].Value+m.Groups[2].Value+m.Groups[3].Value+v+m.Groups[5].Value+m.Groups[6].Value;
  }
  string output=String.Concat(lines); foreach(var kv in p.Values) if(Read(output,kv.Key)!=kv.Value)throw new InvalidDataException("Falha ao conferir "+kv.Key);
  return output;
 }
 public static void ValidateRoot(string root) {
  foreach(string f in new[]{"BF4Launcher.exe","Server\\bf4_Server_Final.exe","Server\\BF4BlazeServer.exe","Server\\dinput8_bf4hook.ini","Server\\dinput8.dll"})
   if(!File.Exists(Path.Combine(root,f)))throw new FileNotFoundException("Pasta do Battlefield 4 incompleta: "+f);
  if(Hash(File.ReadAllBytes(Path.Combine(root,"Server\\dinput8.dll")))!=DllHash)throw new InvalidDataException("A versão da IA mudou. Não vou aplicar perfis sem revisar a compatibilidade.");
 }
 public static string BackupDir(string root) { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BF4BotLauncher","backups",Hash(Encoding.UTF8.GetBytes(Path.GetFullPath(root).TrimEnd('\\').ToLowerInvariant())).Substring(0,16)); }
 public static string Decode(byte[] b){using(var s=new StreamReader(new MemoryStream(b),Encoding.UTF8,true))return s.ReadToEnd();}
 public static string Current(string root) {
  string text=File.ReadAllText(Path.Combine(root,"Server\\dinput8_bf4hook.ini"));
  foreach(var p in Profile.All) if(p.Values.All(k=>Read(text,k.Key)==k.Value))return p.Name;
  return "Personalizado / original";
 }
 public static string Save(string root,Profile p,bool restore,bool light=false) {
  ValidateRoot(root); string path=Path.Combine(root,"Server\\dinput8_bf4hook.ini"); byte[] before=File.ReadAllBytes(path);
  string folder=BackupDir(root); Directory.CreateDirectory(folder); string original=Path.Combine(folder,"original.ini");
  if(restore&&!File.Exists(original))throw new InvalidOperationException("Ainda não existe backup original desta instalação.");
  byte[] after=restore?File.ReadAllBytes(original):new UTF8Encoding(false).GetBytes(light?Performance.Light(Render(Decode(before),p)):Render(Decode(before),p));
  if(!File.Exists(original))File.WriteAllBytes(original,before);
  string backup=Path.Combine(folder,DateTime.Now.ToString("yyyyMMdd-HHmmss-fff")+"-"+Guid.NewGuid().ToString("N").Substring(0,6)+".ini");
  File.WriteAllBytes(backup,before);
  if(Hash(File.ReadAllBytes(path))!=Hash(before))throw new IOException("A configuração mudou durante a operação. Tente novamente.");
  string temp=path+".launcher-"+Guid.NewGuid().ToString("N")+".tmp";
  try { File.WriteAllBytes(temp,after); File.Replace(temp,path,null); } finally {if(File.Exists(temp))File.Delete(temp);}
  if(Hash(File.ReadAllBytes(path))!=Hash(after))throw new IOException("Não foi possível validar a gravação. Backup: "+backup);
  return backup;
 }
}
public static class Server {
 public static List<Process> Find(string root,string exe) {
  string expected=Path.GetFullPath(Path.Combine(root,"Server",exe)); var list=new List<Process>();
  foreach(var p in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exe))) {
   try {if(String.Equals(p.MainModule.FileName,expected,StringComparison.OrdinalIgnoreCase))list.Add(p);} catch {throw new UnauthorizedAccessException("Não consegui conferir o servidor em execução. Reabra este launcher como administrador.");}
  } return list;
 }
 public static string[] PrepareRestart(string root) {
  var servers=Find(root,"bf4_Server_Final.exe"); if(servers.Count>1)throw new InvalidOperationException("Há mais de um servidor desta instalação aberto. Feche os excedentes antes de reiniciar.");
  if(servers.Count==0)return null;
  using(var search=new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId="+servers[0].Id))
   foreach(ManagementObject o in search.Get()) {
    string cmd=(string)o["CommandLine"]; if(String.IsNullOrWhiteSpace(cmd))break;
    var m=Regex.Match(cmd,@"^\s*(?:""[^""]+""|\S+)\s*(.*)$",RegexOptions.Singleline);
    if(m.Success)return new[]{servers[0].Id.ToString(),m.Groups[1].Value};
   }
  throw new InvalidOperationException("Não consegui ler os argumentos do servidor. Salve o perfil e reinicie pelo método que você já usa.");
 }
 public static void Start(string root,string[] restart) {
  if(restart!=null) {
   var p=Process.GetProcessById(Int32.Parse(restart[0])); string expected=Path.GetFullPath(Path.Combine(root,"Server","bf4_Server_Final.exe"));
   if(!String.Equals(p.MainModule.FileName,expected,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("O processo do servidor mudou. Nada foi encerrado.");
   p.Kill(); if(!p.WaitForExit(10000))throw new IOException("O servidor não encerrou. Não iniciei outro.");
  } else if(Find(root,"bf4_Server_Final.exe").Count>0)throw new InvalidOperationException("O servidor já está aberto.");
  if(Find(root,"BF4BlazeServer.exe").Count==0) {
   Process.Start(new ProcessStartInfo(Path.Combine(root,"Server\\BF4BlazeServer.exe")){WorkingDirectory=Path.Combine(root,"Server"),WindowStyle=ProcessWindowStyle.Hidden,UseShellExecute=true});
   System.Threading.Thread.Sleep(2000);
  }
  ProcessStartInfo info;
  if(restart!=null)info=new ProcessStartInfo(Path.Combine(root,"Server\\bf4_Server_Final.exe"),restart[1]);
  else {
   string batch=Path.Combine(root,"Server\\!StartServer.bat"); if(!File.Exists(batch))throw new FileNotFoundException("Script de inicialização não encontrado.");
   info=new ProcessStartInfo(Environment.GetEnvironmentVariable("ComSpec"),"/d /c \"\""+batch+"\"\"");
  }
  info.WorkingDirectory=Path.Combine(root,"Server"); info.UseShellExecute=true; info.WindowStyle=ProcessWindowStyle.Hidden; Process.Start(info);
 }
}
public static class Theme {
 public static Color Background=ColorTranslator.FromHtml("#E8EDF0"), Surface=ColorTranslator.FromHtml("#FFFFFF"), Ink=ColorTranslator.FromHtml("#1E3442"), Muted=ColorTranslator.FromHtml("#4D626E"), Accent=ColorTranslator.FromHtml("#A34D14"), Border=ColorTranslator.FromHtml("#C4D0D7");
 public static Font Body=new Font("Segoe UI",10), Title=new Font("Bahnschrift",25,FontStyle.Bold), Heading=new Font("Bahnschrift",17,FontStyle.Bold);
}
public sealed class StatusLabel : Label { public void Announce(){ AccessibleName=Text; AccessibilityNotifyClients(AccessibleEvents.NameChange,-1); } }
public class Launcher : Form {
 public bool IsBusy {get{return busy;}}
 public Func<Task> StartHandler;
 TextBox path; Label detail,title,summary,current,limit; StatusLabel status; FlowLayoutPanel levels; NumericUpDown bots; Button apply,restart,restore,game,elevate; Profile selected=Profile.All[2]; bool busy; CheckBox light;
 public Launcher(string initial) {
  Text="BF4 • Controle de bots"; MinimumSize=new Size(960,760); ClientSize=new Size(1030,800); StartPosition=FormStartPosition.CenterScreen; Font=Theme.Body; BackColor=Theme.Background; ForeColor=Theme.Ink; AutoScaleMode=AutoScaleMode.Dpi;
  var shell=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(26),ColumnCount=1,RowCount=7}; shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); Controls.Add(shell);
  foreach(int h in new[]{73,70,64,0,70,94,70})shell.RowStyles.Add(h==0?new RowStyle(SizeType.Percent,100):new RowStyle(SizeType.Absolute,h));
  var heading=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2}; heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));heading.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,220));
  heading.Controls.Add(new Label{Text="BF4 / Controle de bots",Font=Theme.Title,AutoSize=true,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,0);
  elevate=MakeButton("Reabrir como administrador",false);elevate.Click+=(s,e)=>Elevate(); heading.Controls.Add(elevate,1,0); shell.Controls.Add(heading,0,0);
  var folder=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=2};folder.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));folder.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,120));folder.RowStyles.Add(new RowStyle(SizeType.Absolute,24));folder.RowStyles.Add(new RowStyle(SizeType.Percent,100));
  folder.Controls.Add(new Label{Text="Pasta do Battlefield 4",AutoSize=true},0,0); path=new TextBox{Text=initial,Dock=DockStyle.Fill,AccessibleName="Pasta do Battlefield 4"}; folder.Controls.Add(path,0,1);var browse=MakeButton("Selecionar…",false);browse.Click+=(s,e)=>{using(var d=new FolderBrowserDialog{Description="Selecione a pasta que contém BF4Launcher.exe",SelectedPath=path.Text})if(d.ShowDialog(this)==DialogResult.OK){path.Text=d.SelectedPath;RefreshCurrent();}};folder.Controls.Add(browse,1,1);shell.Controls.Add(folder,0,1);path.Leave+=(s,e)=>RefreshCurrent();
  levels=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,Padding=new Padding(0,8,0,0)};
  foreach(var p in Profile.All){var radio=new RadioButton{Text=p.Name,Appearance=Appearance.Button,TextAlign=ContentAlignment.MiddleCenter,Width=178,Height=42,Margin=new Padding(0,0,8,0),FlatStyle=FlatStyle.Flat,AccessibleName="Dificuldade "+p.Name,Tag=p,Cursor=Cursors.Hand};radio.FlatAppearance.BorderColor=Theme.Border;radio.FlatAppearance.CheckedBackColor=Theme.Ink;radio.UseVisualStyleBackColor=false;radio.CheckedChanged+=(s,e)=>{if(radio.Checked){selected=(Profile)radio.Tag;ShowProfile();}};levels.Controls.Add(radio);}shell.Controls.Add(levels,0,2); levels.SizeChanged+=(s,e)=>{foreach(Control c in levels.Controls)c.Width=Math.Max(90,(levels.ClientSize.Width-40)/5);};
  var card=new TableLayoutPanel{Dock=DockStyle.Fill,BackColor=Theme.Surface,Padding=new Padding(22),RowCount=5,ColumnCount=1};card.RowStyles.Add(new RowStyle(SizeType.Absolute,38));card.RowStyles.Add(new RowStyle(SizeType.Absolute,33));card.RowStyles.Add(new RowStyle(SizeType.Percent,100));card.RowStyles.Add(new RowStyle(SizeType.Absolute,60));card.RowStyles.Add(new RowStyle(SizeType.Absolute,35));
  title=new Label{Font=Theme.Heading,Dock=DockStyle.Fill};summary=new Label{Dock=DockStyle.Fill,ForeColor=Theme.Accent}; detail=new Label{Dock=DockStyle.Fill,AutoEllipsis=false,Padding=new Padding(0,8,0,0)};
  limit=new Label{Text="Mira e cadência dos tiros continuam no padrão do mod.",Dock=DockStyle.Fill,ForeColor=Theme.Muted};current=new Label{Dock=DockStyle.Fill,ForeColor=Theme.Muted};card.Controls.Add(title,0,0);card.Controls.Add(summary,0,1);card.Controls.Add(detail,0,2);var modePanel=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=2,ColumnCount=1};modePanel.RowStyles.Add(new RowStyle(SizeType.Absolute,26));modePanel.RowStyles.Add(new RowStyle(SizeType.Percent,100));modePanel.Controls.Add(limit,0,0);light=new CheckBox{Text="Modo leve: menos granadas e suprimentos; sem chats e ping cosméticos",Checked=true,Dock=DockStyle.Fill,AccessibleName="Modo leve de desempenho"};modePanel.Controls.Add(light,0,1);card.Controls.Add(modePanel,0,3);card.Controls.Add(current,0,4);shell.Controls.Add(card,0,3);
  var countPanel=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(0,17,0,0)};countPanel.Controls.Add(new Label{Text="Quantidade de bots",Width=160,Height=32,TextAlign=ContentAlignment.MiddleLeft});bots=new NumericUpDown{Minimum=0,Maximum=128,Value=20,Width=75,AccessibleName="Quantidade total de bots"};countPanel.Controls.Add(bots);var copy=MakeButton("Copiar !setbot 20",false);copy.Width=180;copy.Height=34;bots.ValueChanged+=(s,e)=>copy.Text="Copiar !setbot "+bots.Value;copy.Click+=(s,e)=>{try{Clipboard.SetText("!setbot "+bots.Value);SetStatus("Comando copiado. Cole no chat da partida. A quantidade não foi alterada automaticamente.",false);}catch(Exception ex){SetStatus(ex.Message,true);}};countPanel.Controls.Add(copy);countPanel.Controls.Add(new Label{Text="20 bots foi a quantidade estável no seu teste.",AutoSize=true,Padding=new Padding(12,7,0,0),ForeColor=Theme.Muted});shell.Controls.Add(countPanel,0,4);
  var actions=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=2,ColumnCount=4};for(int i=0;i<4;i++)actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,25));actions.RowStyles.Add(new RowStyle(SizeType.Absolute,44));actions.RowStyles.Add(new RowStyle(SizeType.Percent,100));
  apply=MakeButton("Salvar perfil",true);restart=MakeButton("Aplicar e reiniciar servidor",true);restore=MakeButton("Restaurar original",false);game=MakeButton("Iniciar tudo e jogar",true);apply.Click+=async(s,e)=>await Apply(false,false);restart.Click+=async(s,e)=>await Apply(true,false);restore.Click+=async(s,e)=>await Apply(false,true);game.Click+=async(s,e)=>{if(StartHandler!=null)await StartHandler();else await OpenGame();};actions.Controls.Add(apply,0,0);actions.Controls.Add(restart,1,0);actions.Controls.Add(restore,2,0);actions.Controls.Add(game,3,0);var note=new Label{Text="Iniciar tudo abre backend, servidor e jogo; fecha o EA App se ele ocupar a porta local. Reiniciar interrompe a partida.",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,ForeColor=Theme.Muted};actions.Controls.Add(note,0,1);actions.SetColumnSpan(note,4);shell.Controls.Add(actions,0,5);
  status=new StatusLabel{Dock=DockStyle.Fill,Padding=new Padding(8),BackColor=Theme.Surface,AccessibleName="Estado da operação"};shell.Controls.Add(status,0,6);
  ((RadioButton)levels.Controls[2]).Checked=true;RefreshCurrent();SetStatus("Escolha um perfil. Nada será alterado até você salvar ou aplicar.",false);
  FormClosing+=(s,e)=>{if(busy){e.Cancel=true;SetStatus("Aguarde a operação terminar antes de fechar.",true);}};
 }
 Button MakeButton(string text,bool primary){var b=new Button{Text=text,Dock=DockStyle.Fill,FlatStyle=FlatStyle.Flat,Margin=new Padding(0,0,8,0),Cursor=Cursors.Hand,AutoEllipsis=false,BackColor=primary?Theme.Ink:Theme.Surface,ForeColor=primary?Color.White:Theme.Ink};b.FlatAppearance.BorderColor=Theme.Border;b.FlatAppearance.MouseOverBackColor=primary?Theme.Accent:Theme.Background;return b;}
 void ShowProfile(){title.Text=selected.Name;summary.Text=selected.Summary;detail.Text=selected.Detail;foreach(RadioButton r in levels.Controls){r.BackColor=r.Checked?Theme.Ink:Theme.Surface;r.ForeColor=r.Checked?Color.White:Theme.Ink;}}
 void SetStatus(string text,bool error){status.Text=text;status.ForeColor=error?Color.Maroon:Theme.Ink;status.Announce();}
 void RefreshCurrent(){try{current.Text="Configurado: "+Config.Current(path.Text)+" • salvar não confirma ativação na partida";}catch{current.Text="Configuração não encontrada. Confira a pasta do jogo.";}}
 public bool Confirm(string titleText,string message,string action){using(var d=new Form{Text=titleText,ClientSize=new Size(530,210),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,BackColor=Theme.Background,Font=Theme.Body}){var l=new Label{Text=message,Location=new Point(22,22),Size=new Size(486,115)};var cancel=new Button{Text="Cancelar",DialogResult=DialogResult.Cancel,Location=new Point(225,158),Size=new Size(120,34)};var ok=new Button{Text=action,DialogResult=DialogResult.OK,Location=new Point(355,158),Size=new Size(150,34)};d.Controls.Add(l);d.Controls.Add(cancel);d.Controls.Add(ok);d.CancelButton=cancel;d.AcceptButton=cancel;d.Shown+=(s,e)=>cancel.Focus();return d.ShowDialog(this)==DialogResult.OK;}}
 async Task Apply(bool restartServer,bool restoreOriginal){if(busy)return; if(restartServer&&!Confirm("Reiniciar servidor local","O perfil será salvo e o servidor desta instalação será reiniciado. A partida local em andamento será interrompida. O jogo e o backend não serão encerrados.","Aplicar e reiniciar"))return;if(restoreOriginal&&!Confirm("Restaurar configuração original","A primeira configuração salva em backup por este launcher será restaurada. Também será criado um backup da configuração atual. O servidor continuará aberto; reinicie-o para ativar a restauração.","Restaurar"))return;
  string root=path.Text;Profile profile=selected;bool lightweight=light.Checked;busy=true;light.Enabled=false;foreach(var b in new[]{apply,restart,restore,game,elevate})b.Enabled=false;levels.Enabled=false;path.Enabled=false;SetStatus("Conferindo configuração e preparando backup…",false);bool saved=false;
  try {string backup=await Task.Run(()=>{Config.ValidateRoot(root);var args=restartServer?Server.PrepareRestart(root):null;string b=Config.Save(root,profile,restoreOriginal,lightweight);saved=true;if(restartServer)Server.Start(root,args);return b;});RefreshCurrent();SetStatus(restartServer?"Perfil salvo; reinício solicitado. Aguarde o servidor carregar e reconecte pelo jogo. Backup: "+backup:"Configuração salva. Reinicie o servidor para ativar. Backup: "+backup,false);}
  catch(Exception ex){SetStatus((saved?"Configuração salva, mas o reinício falhou. ":"Nada foi aplicado ao servidor. ")+ex.Message+(ex is UnauthorizedAccessException?" Use Reabrir como administrador.":""),true);}
  finally{busy=false;light.Enabled=true;foreach(var b in new[]{apply,restart,restore,game,elevate})b.Enabled=true;levels.Enabled=true;path.Enabled=true;}
 }
 public async Task OpenGame(){if(busy)return;var principal=new WindowsPrincipal(WindowsIdentity.GetCurrent());if(!principal.IsInRole(WindowsBuiltInRole.Administrator)){try{Process.Start(new ProcessStartInfo(Application.ExecutablePath,"--play --game \""+path.Text+"\""){UseShellExecute=true,Verb="runas"});Close();}catch(System.ComponentModel.Win32Exception ex){SetStatus(ex.NativeErrorCode==1223?"O início foi cancelado na confirmação do Windows. Nenhum jogo foi aberto.":ex.Message,true);}return;}
  string root=path.Text;busy=true;foreach(var b in new[]{apply,restart,restore,game,elevate})b.Enabled=false;path.Enabled=false;light.Enabled=false;levels.Enabled=false;
  try{Action<string> progress=t=>{if(!IsDisposed)BeginInvoke(new Action(()=>SetStatus(t,false)));};string result=await Task.Run(()=>OneClick.Run(root,progress));SetStatus(result,false);File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"tests","oneclick-last-run.txt"),DateTime.Now.ToString("s")+"\r\n"+result,Encoding.UTF8);}catch(Exception ex){SetStatus("Inicialização interrompida: "+ex.Message,true);File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"tests","oneclick-last-run.txt"),"FALHA: "+ex.Message,Encoding.UTF8);}finally{busy=false;foreach(var b in new[]{apply,restart,restore,game,elevate})b.Enabled=true;path.Enabled=true;light.Enabled=true;levels.Enabled=true;}}
 void Elevate(){try{Process.Start(new ProcessStartInfo(Application.ExecutablePath,"--game \""+path.Text+"\""){UseShellExecute=true,Verb="runas"});Close();}catch(System.ComponentModel.Win32Exception ex){SetStatus(ex.NativeErrorCode==1223?"A abertura como administrador foi cancelada. Nenhuma alteração foi feita.":ex.Message,true);}}
}
public static class Program {
 [STAThread] public static int Main(string[] args){
  if(args.Length>0&&args[0]=="--oneclick-test")return OneClick.Test(args[1]);
  if(args.Length>0&&args[0]=="--performance-test"){ Performance.Test("C:\\Program Files\\EA Games\\Battlefield 4",args[1]);return 0;}
  if(args.Length>0&&args[0]=="--self-test")return Tests.Run(args.Length>1?args[1]:Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-results.txt"));
  Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);string root="C:\\Program Files\\EA Games\\Battlefield 4";for(int i=0;i+1<args.Length;i++)if(args[i]=="--game")root=args[i+1];var form=new Launcher(root);if(args.Contains("--play"))form.Shown+=async(s,e)=>await form.OpenGame();Application.Run(form);return 0;
 }
}
public static class Tests {
 public static int Run(string report){var results=new List<string>();try{
  string ini="; preservado\r\n[bf4hook]\r\n"+String.Join("\r\n",Profile.All[2].Values.Select(k=>k.Key+"="+k.Value))+"\r\nExternalIP=127.0.0.1\r\nChatCommands=1\r\nEnableAim=1\r\nEnableFire=1\r\n[other]\r\nObjectiveShare=keep\r\n";
  foreach(var p in Profile.All){string rendered=Config.Render(ini,p);foreach(var kv in p.Values)if(Config.Read(rendered,kv.Key)!=kv.Value)throw new Exception(p.Name+" / "+kv.Key);if(!rendered.Contains("ExternalIP=127.0.0.1\r\n")||!rendered.Contains("EnableAim=1\r\nEnableFire=1")||!rendered.Contains("[other]\r\nObjectiveShare=keep"))throw new Exception("Chaves externas foram alteradas");if(Config.Render(rendered,p)!=rendered)throw new Exception("Aplicação não idempotente");results.Add("PASS perfil "+p.Name+": chaves, preservação e idempotência");}
  bool rejected=false;try{Config.Render(ini.Replace("BotMedicRevive=1\r\n",""),Profile.All[0]);}catch(InvalidDataException){rejected=true;}if(!rejected)throw new Exception("Aceitou chave ausente");results.Add("PASS rejeita chave ausente");
  rejected=false;try{Config.Render(ini.Replace("EnableAim=1","ObjectiveShare=0.4\r\nEnableAim=1"),Profile.All[0]);}catch(InvalidDataException){rejected=true;}if(!rejected)throw new Exception("Aceitou chave duplicada");results.Add("PASS rejeita chave duplicada");
  string lf=ini.Replace("\r\n","\n");if(Config.Render(lf,Profile.All[0]).Contains("\r"))throw new Exception("Alterou finais de linha");results.Add("PASS preserva LF e CRLF");
  string commented=ini.Replace("ObjectiveShare=0.60","ObjectiveShare=0.60 ; comentario");if(!Config.Render(commented,Profile.All[0]).Contains("; comentario"))throw new Exception("Perdeu comentário");results.Add("PASS preserva comentários");
  var fake=Path.Combine(Path.GetTempPath(),"BF4Bots-tests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(fake);File.WriteAllText(Path.Combine(fake,"marker.txt"),"fixture");rejected=false;try{Config.ValidateRoot(fake);}catch(FileNotFoundException){rejected=true;}if(!rejected)throw new Exception("Aceitou instalação incompleta");results.Add("PASS rejeita instalação incompleta");
  string installed="C:\\Program Files\\EA Games\\Battlefield 4";
  if(File.Exists(Path.Combine(installed,"Server\\dinput8.dll"))) {
   Directory.CreateDirectory(Path.Combine(fake,"Server")); foreach(string f in new[]{"BF4Launcher.exe","Server\\bf4_Server_Final.exe","Server\\BF4BlazeServer.exe"})File.WriteAllText(Path.Combine(fake,f),"fixture only");
   File.Copy(Path.Combine(installed,"Server\\dinput8.dll"),Path.Combine(fake,"Server\\dinput8.dll"));
   byte[] original=File.ReadAllBytes(Path.Combine(installed,"Server\\dinput8_bf4hook.ini"));string fixturePath=Path.Combine(fake,"Server\\dinput8_bf4hook.ini");File.WriteAllBytes(fixturePath,original);
   Config.Save(fake,Profile.All[0],false);if(Config.Current(fake)!="Noob")throw new Exception("Perfil não foi gravado");
   Config.Save(fake,Profile.All[4],false);if(Config.Current(fake)!="Extremo")throw new Exception("Segundo perfil não foi gravado");
   Config.Save(fake,null,true);if(Config.Hash(File.ReadAllBytes(fixturePath))!=Config.Hash(original))throw new Exception("Restauração não foi exata");results.Add("PASS integração em cópia temporária: backup, dois perfis e restauração byte a byte; instalação real sem alterações");
  }
  Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
  using(var form=new Launcher("C:\\Program Files\\EA Games\\Battlefield 4")) {
   form.Show();form.PerformLayout();Application.DoEvents();
   var radios=form.Controls.Cast<Control>().SelectMany(c=>c.Controls.Cast<Control>()).OfType<FlowLayoutPanel>().SelectMany(c=>c.Controls.Cast<Control>()).OfType<RadioButton>().ToArray();
   if(radios.Length!=5)throw new Exception("A interface não contém cinco perfis");
   foreach(var radio in radios){radio.Checked=true;Application.DoEvents();if(radio.BackColor!=Theme.Ink||radio.ForeColor!=Color.White)throw new Exception("Seleção visual incorreta");}radios[2].Checked=true;Application.DoEvents();results.Add("PASS seleção dos cinco níveis na interface");
   using(var bitmap=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));bitmap.Save(Path.Combine(Path.GetDirectoryName(report),"preview.png"));}
   form.ClientSize=new Size(960,760);form.PerformLayout();Application.DoEvents();using(var bitmap=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));bitmap.Save(Path.Combine(Path.GetDirectoryName(report),"preview-small.png"));}form.Close();
  }results.Add("PASS instanciação e renderização da interface em dois tamanhos; sem aplicar configurações");
  File.WriteAllLines(report,results,Encoding.UTF8);return 0;
 }catch(Exception ex){results.Add("FAIL "+ex);File.WriteAllLines(report,results,Encoding.UTF8);return 1;}}
}
}
