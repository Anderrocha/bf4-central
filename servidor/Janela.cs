using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace BF4Servidor {
public sealed class Painel : Form {
 readonly Engine engine;readonly Pedidos pedidos=new Pedidos();
 readonly Dot dot;readonly Label state,detail,ip,lastRequest;readonly PillButton toggle,copy;readonly Switch accept,light,autostart;readonly StatusStrip2 status;readonly NotifyIcon tray;readonly System.Windows.Forms.Timer timer;
 bool busy,quitting,startedByRequest;readonly bool startHidden;
 public Painel(string root,bool hidden){
  engine=new Engine(root);startHidden=hidden;
  Text="BF4 • Servidor dedicado";ClientSize=new Size(600,760);MinimumSize=new Size(560,780);StartPosition=FormStartPosition.CenterScreen;BackColor=Theme.Background;ForeColor=Theme.Ink;Font=Theme.Body;AutoScaleMode=AutoScaleMode.Dpi;
  try{Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);}catch{}
  var header=new Header(Art()){Dock=DockStyle.Top,Height=150,Title="BF4 / Servidor dedicado",Subtitle="Este PC roda o servidor com bots. A BF4 Central do PC de jogo pede, você aceita, e o resto é automático."};
  var body=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(22,18,22,14),ColumnCount=1,RowCount=4};
  foreach(int h in new[]{122,66,0,52})body.RowStyles.Add(h==0?new RowStyle(SizeType.Percent,100):new RowStyle(SizeType.Absolute,h));
  // Cartão de estado: bolinha + estado + detalhe | IP + copiar
  var stateCard=new Card{Dock=DockStyle.Fill};var sg=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=2,BackColor=Theme.Surface};
  sg.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,30));sg.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));sg.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,170));sg.RowStyles.Add(new RowStyle(SizeType.Absolute,46));sg.RowStyles.Add(new RowStyle(SizeType.Percent,100));
  dot=new Dot{Anchor=AnchorStyles.Left,Margin=new Padding(0,6,0,0)};sg.Controls.Add(dot,0,0);
  state=new Label{Dock=DockStyle.Fill,Font=Theme.Big,TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true};sg.Controls.Add(state,1,0);
  detail=new Label{Dock=DockStyle.Fill,ForeColor=Theme.Muted,AutoEllipsis=true};sg.Controls.Add(detail,1,1);
  var ipBox=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,BackColor=Theme.Surface,Margin=Padding.Empty};ipBox.RowStyles.Add(new RowStyle(SizeType.Absolute,20));ipBox.RowStyles.Add(new RowStyle(SizeType.Absolute,30));ipBox.RowStyles.Add(new RowStyle(SizeType.Percent,100));
  ipBox.Controls.Add(new Label{Text="IP DESTE PC",Dock=DockStyle.Fill,ForeColor=Theme.Accent,Font=new Font("Segoe UI",8.5f,FontStyle.Bold),TextAlign=ContentAlignment.BottomRight},0,0);
  ip=new Label{Dock=DockStyle.Fill,Font=Theme.Heading,TextAlign=ContentAlignment.MiddleRight};ipBox.Controls.Add(ip,0,1);
  copy=new PillButton{Text="Copiar IP",Anchor=AnchorStyles.Top|AnchorStyles.Right,Size=new Size(110,32),Font=Theme.Body,Margin=new Padding(0,4,0,0)};copy.Click+=(s,e)=>{var a=Engine.LanIp();if(a!=null){Clipboard.SetText(a);Say("IP copiado: "+a,Theme.Success);}};ipBox.Controls.Add(copy,0,2);
  sg.Controls.Add(ipBox,2,0);sg.SetRowSpan(ipBox,2);stateCard.Controls.Add(sg);body.Controls.Add(stateCard,0,0);
  toggle=new PillButton{Dock=DockStyle.Fill,Font=new Font("Segoe UI",12.5f,FontStyle.Bold),ForeColor=Color.White,Margin=new Padding(0,0,0,14)};toggle.Click+=async(s,e)=>await Toggle();body.Controls.Add(toggle,0,1);
  // Cartão da conexão com a Central
  var link=new Card{Dock=DockStyle.Fill};var lg=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=5,BackColor=Theme.Surface};
  foreach(int h in new[]{36,64,64,64,0})lg.RowStyles.Add(h==0?new RowStyle(SizeType.Percent,100):new RowStyle(SizeType.Absolute,h));
  lg.Controls.Add(new Label{Text="Conexão com a BF4 Central",Font=Theme.Heading,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,0);
  accept=new Switch{Caption="Aceitar pedidos da Central",Hint="Quando a Central pedir, aparece uma janela aqui para você aceitar ou recusar.",Dock=DockStyle.Fill,BackColor=Theme.Surface,Checked=Flag("aceitar-pedidos.txt",true)};
  accept.CheckedChanged+=(s,e)=>{pedidos.Accepting=accept.Checked;SetFlag("aceitar-pedidos.txt",accept.Checked);Say(accept.Checked?"Pedidos da Central ativados.":"Pedidos da Central desativados: a Central vai usar o próprio PC.",Theme.Ink);};lg.Controls.Add(accept,0,1);
  light=new Switch{Caption="Modo leve",Hint="Prioridade baixa: o dono usa o PC sem travar; os bots podem reagir um pouco mais devagar.",Dock=DockStyle.Fill,BackColor=Theme.Surface,Checked=Flag("modo-leve.txt",false)};
  light.CheckedChanged+=(s,e)=>{SetFlag("modo-leve.txt",light.Checked);engine.SetPriority(light.Checked);};lg.Controls.Add(light,0,2);
  autostart=new Switch{Caption="Abrir com o Windows",Hint="Ao entrar no Windows, o painel abre perto do relógio, pronto para os pedidos.",Dock=DockStyle.Fill,BackColor=Theme.Surface,Checked=Startup.Enabled()};
  autostart.Click+=(s,e)=>{try{Startup.Set(autostart.Checked);Say(autostart.Checked?"O painel vai abrir junto com o Windows.":"O painel não abre mais com o Windows.",Theme.Success);}catch(Exception ex){autostart.Checked=Startup.Enabled();Say(ex.Message,Theme.Danger);}};lg.Controls.Add(autostart,0,3);
  lastRequest=new Label{Dock=DockStyle.Fill,ForeColor=Theme.Muted,Text="Nenhum pedido recebido ainda.",TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true};lg.Controls.Add(lastRequest,0,4);
  link.Controls.Add(lg);body.Controls.Add(link,0,2);
  status=new StatusStrip2{Dock=DockStyle.Fill,Margin=Padding.Empty};body.Controls.Add(status,0,3);
  Controls.Add(body);Controls.Add(header);body.BringToFront();
  tray=new NotifyIcon{Icon=Icon,Text="BF4 Servidor",Visible=true};var menu=new ContextMenuStrip();menu.Items.Add("Abrir painel",null,(s,e)=>ShowPanel());menu.Items.Add("Sair do painel",null,(s,e)=>Quit());tray.ContextMenuStrip=menu;tray.DoubleClick+=(s,e)=>ShowPanel();
  Resize+=(s,e)=>{if(WindowState==FormWindowState.Minimized&&accept.Checked){Hide();tray.ShowBalloonTip(3000,"BF4 Servidor","O painel continua aqui, esperando pedidos da Central.",ToolTipIcon.Info);}};
  FormClosing+=OnClosing;
  timer=new System.Windows.Forms.Timer{Interval=2000};timer.Tick+=(s,e)=>Refresh2();timer.Start();
  pedidos.Accepting=accept.Checked;pedidos.Log=t=>Say(t,Theme.Ink);pedidos.State=()=>state.Text;pedidos.Ask=AskOwner;pedidos.StartServer=StartForRequest;pedidos.StopIfOurs=StopForRelease;
  Load+=(s,e)=>Boot();
  Refresh2();
  if(!Directory.Exists(engine.ServerDir))Say("Pasta Server não encontrada ao lado do painel. Copie o pacote inteiro.",Theme.Danger);else Say("Pronto. Deixe este painel aberto para a Central conseguir pedir o servidor.",Theme.Ink);
 }
 protected override void SetVisibleCore(bool value){if(startHidden&&!IsHandleCreated){CreateHandle();value=false;OnLoad(EventArgs.Empty);}base.SetVisibleCore(value);}
 static Image Art(){try{using(var s=typeof(Painel).Assembly.GetManifestResourceStream("BF4Servidor.Banner")){if(s!=null)return new Bitmap(Image.FromStream(s));}}catch{}return null;}
 bool Flag(string file,bool fallback){try{string f=Path.Combine(engine.Root,file);return File.Exists(f)?File.ReadAllText(f).Trim()=="1":fallback;}catch{return fallback;}}
 void SetFlag(string file,bool v){try{File.WriteAllText(Path.Combine(engine.Root,file),v?"1":"0");}catch{}}
 HashSet<string> Trusted(){try{string f=Path.Combine(engine.Root,"pcs-confiaveis.txt");return new HashSet<string>(File.Exists(f)?File.ReadAllLines(f).Select(x=>x.Trim()).Where(x=>x.Length>0):new string[0]);}catch{return new HashSet<string>();}}
 void Trust(string address){var t=Trusted();if(t.Add(address))try{File.WriteAllLines(Path.Combine(engine.Root,"pcs-confiaveis.txt"),t.ToArray());}catch{}}
 bool booted;
 void Boot(){
  if(booted)return;booted=true;
  Task.Run(()=>{
   try{engine.EnsureFirewall(t=>Say(t,Theme.Accent));}catch(Exception ex){Say(ex.Message,Theme.Danger);}
   try{pedidos.Start();}catch(Exception ex){Say("Não consegui abrir a porta "+Pedidos.ControlPort+" para os pedidos: "+ex.Message,Theme.Danger);}
  });
 }
 void ShowPanel(){Show();WindowState=FormWindowState.Normal;Activate();}
 void Quit(){quitting=true;Close();}
 void OnClosing(object sender,FormClosingEventArgs e){
  if(busy){e.Cancel=true;Say("Aguarde terminar de ligar/desligar.",Theme.Accent);return;}
  if(!quitting&&e.CloseReason==CloseReason.UserClosing&&accept.Checked){e.Cancel=true;Hide();tray.ShowBalloonTip(3000,"BF4 Servidor","O painel ficou perto do relógio, esperando pedidos. Para sair de vez: botão direito no ícone → Sair do painel.",ToolTipIcon.Info);return;}
  if(engine.Running(Engine.Server).Count>0&&e.CloseReason==CloseReason.UserClosing&&MessageBox.Show(this,"O servidor continua ligado em segundo plano. Fechar só o painel?","BF4 Servidor",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes){e.Cancel=true;quitting=false;return;}
  pedidos.Dispose();tray.Visible=false;tray.Dispose();timer.Dispose();
 }
 void Say(string text,Color mark){if(IsDisposed)return;if(InvokeRequired){try{BeginInvoke(new Action(()=>Say(text,mark)));}catch{}return;}status.Mark=mark;status.Text=text;}
 int SafeRcon(){try{return engine.RconPort();}catch{return 25210;}}
 void Refresh2(){
  if(busy)return;
  bool backend=engine.Running(Engine.Backend).Count>0,server=engine.Running(Engine.Server).Count>0,ready=server&&Engine.Listening(SafeRcon());
  ip.Text=Engine.LanIp()??"sem rede";
  if(ready){state.Text="Ligado";dot.Color=Theme.Success;dot.Hollow=false;toggle.Text="Desligar servidor";toggle.BackColor=Theme.Danger;}
  else if(backend||server){state.Text="Ligando ou incompleto";dot.Color=Theme.Warning;dot.Hollow=false;toggle.Text="Desligar servidor";toggle.BackColor=Theme.Danger;}
  else{state.Text="Desligado";dot.Color=Theme.Muted;dot.Hollow=true;toggle.Text="Ligar servidor";toggle.BackColor=Theme.Ink;}
  detail.Text="Backend "+(backend?"ligado":"desligado")+"  •  Servidor com bots "+(server?(ready?"pronto":"abrindo"):"desligado")+(startedByRequest&&ready?"\r\nLigado a pedido da Central":"");
  dot.Invalidate();tray.Text="BF4 Servidor • "+state.Text;
 }
 void Busy(bool on,string text){busy=on;toggle.Enabled=!on;toggle.Busy=on;if(on){toggle.Text=text;toggle.BackColor=Theme.Muted;}}
 async Task Toggle(){
  if(busy)return;bool on=engine.Running(Engine.Server).Count>0||engine.Running(Engine.Backend).Count>0;
  Busy(true,on?"Desligando…":"Ligando…");
  try{
   if(on){await Task.Run(()=>engine.Stop(t=>Say(t,Theme.Accent)));startedByRequest=false;Say("Servidor desligado. O processador deste PC está livre.",Theme.Success);}
   else{bool l=light.Checked;await Task.Run(()=>engine.Start(l,t=>Say(t,Theme.Accent)));Say("Servidor ligado. No PC de jogo, use Conectar ao PC servidor na Central.",Theme.Success);}
  }catch(Exception ex){Say(ex.Message,Theme.Danger);}
  finally{Busy(false,null);Refresh2();}
 }
 // Chamado pela thread de rede: mostra a janela do pedido na thread da interface e espera a resposta.
 bool AskOwner(string who,string address){
  bool ok=false;
  Invoke(new Action(()=>{
   if(Trusted().Contains(address)){ok=true;lastRequest.Text="Último pedido: "+who+" ("+address+") às "+DateTime.Now.ToString("HH:mm")+" • aceito automaticamente";tray.ShowBalloonTip(4000,"BF4 Servidor",who+" pediu o servidor; aceito automaticamente (PC confiável).",ToolTipIcon.Info);return;}
   if(!Visible)tray.ShowBalloonTip(4000,"BF4 Servidor","Pedido de "+who+" para ligar o servidor.",ToolTipIcon.Info);
   using(var ask=new PedidoForm(who,address,Icon)){ok=ask.ShowDialog()==DialogResult.Yes;if(ok&&ask.Always.Checked)Trust(address);}
   lastRequest.Text="Último pedido: "+who+" ("+address+") às "+DateTime.Now.ToString("HH:mm")+" • "+(ok?"aceito":"recusado");
  }));
  return ok;
 }
 string StartForRequest(Action<string> remote){
  Invoke(new Action(()=>Busy(true,"Ligando a pedido da Central…")));
  try{
   bool l=false;Invoke(new Action(()=>l=light.Checked));
   engine.Start(l,t=>{Say(t,Theme.Accent);remote(t);});startedByRequest=true;
   string address=Engine.LanIp();if(address==null)throw new IOException("Este PC saiu da rede.");
   return address+"|"+engine.RconPort();
  }finally{Invoke(new Action(()=>{Busy(false,null);Refresh2();}));}
 }
 bool StopForRelease(){
  if(!startedByRequest)return false;
  Invoke(new Action(()=>Busy(true,"Desligando…")));
  try{engine.Stop(t=>Say(t,Theme.Accent));startedByRequest=false;return true;}
  finally{Invoke(new Action(()=>{Busy(false,null);Refresh2();}));}
 }
}
// "Abrir com o Windows": tarefa agendada no logon com privilégio de administrador (sem pedir UAC a cada vez).
static class Startup {
 const string Task="BF4 Servidor (painel)";
 static int Run(string args){var info=new ProcessStartInfo("schtasks.exe",args){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};using(var p=Process.Start(info)){p.StandardOutput.ReadToEnd();p.StandardError.ReadToEnd();p.WaitForExit();return p.ExitCode;}}
 public static bool Enabled(){try{return Run("/Query /TN \""+Task+"\"")==0;}catch{return false;}}
 public static void Set(bool on){
  int code=on?Run("/Create /F /TN \""+Task+"\" /SC ONLOGON /RL HIGHEST /TR \"\\\""+Application.ExecutablePath+"\\\" --tray\""):Run("/Delete /F /TN \""+Task+"\"");
  if(code!=0)throw new IOException(on?"Não consegui criar a inicialização automática.":"Não consegui remover a inicialização automática.");
 }
}
}
