using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
namespace BF4Bots {
// Tela final interativa (20/11): vídeo grande; ao terminar, o jogo inicia sozinho. "Pular vídeo" inicia na hora;
// a chave "Iniciar o jogo ao fim do vídeo" desliga o início automático (fica salva).
public sealed partial class Central {
 PerformanceSwitch introAutoStart;Label introHint;ActionButton skipIntro;Timer introTick;bool introWatching;
 static string AutoStartFile{get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BF4BotLauncher","intro-autostart.txt");}}
 public static bool IntroAutoStartEnabled{
  get{try{return !File.Exists(AutoStartFile)||File.ReadAllText(AutoStartFile).Trim()!="0";}catch{return true;}}
  set{try{Directory.CreateDirectory(Path.GetDirectoryName(AutoStartFile));File.WriteAllText(AutoStartFile,value?"1":"0");}catch{}}
 }
 void BuildIntroControls(TableLayoutPanel stack){
  skipIntro=new ActionButton{Text="Pular vídeo ⏭",BackColor=Theme.Surface,ForeColor=Theme.Ink,Width=150,Height=40,Margin=new Padding(0,6,0,6),Visible=false,AccessibleName="Pular o vídeo e iniciar o jogo"};
  skipIntro.Click+=async(s,e)=>await StartPrepared();
  introAutoStart=new PerformanceSwitch{Caption="Iniciar o jogo ao fim do vídeo",Checked=IntroAutoStartEnabled,BackColor=Theme.Background,Anchor=AnchorStyles.None,Width=330,Height=34,Margin=new Padding(0,2,0,0),AccessibleName="Iniciar o jogo ao fim do vídeo"};
  introAutoStart.CheckedChanged+=(s,e)=>{IntroAutoStartEnabled=introAutoStart.Checked;introWatching=introAutoStart.Checked&&intro!=null&&intro.Playing&&finalView;RefreshIntroControls();};
  introHint=new Label{Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,ForeColor=Theme.Muted};
  stack.Controls.Add(introAutoStart,0,7);stack.Controls.Add(introHint,0,8);
  introTick=new Timer{Interval=500};introTick.Tick+=(s,e)=>RefreshIntroControls();FormClosed+=(s,e)=>introTick.Dispose();
  RefreshIntroControls();
 }
 void BeginIntroWatch(){introWatching=IntroAutoStartEnabled&&finalView;if(introTick!=null)introTick.Start();RefreshIntroControls();}
 // Ao iniciar (botão, pular ou fim do vídeo): para o vídeo e o som, mantendo o último quadro.
 void StopIntroWatch(){introWatching=false;if(intro!=null&&intro.Playing)intro.Stop(true);RefreshIntroControls();}
 void RefreshIntroControls(){
  if(introHint==null)return;bool playing=intro!=null&&intro.Playing&&finalView;
  skipIntro.Visible=playing;if(!playing&&introTick!=null)introTick.Stop();
  if(!IntroPlayer.Available){introAutoStart.Visible=false;introHint.Text="";return;}
  introAutoStart.Visible=true;
  introHint.Text=!introAutoStart.Checked?L.T("Desligado: o vídeo toca e você inicia quando quiser."):playing&&introWatching?L.T("O jogo começa sozinho quando o vídeo terminar")+" • "+intro.SecondsLeft+" s":L.T("O jogo começa sozinho quando o vídeo terminar");
 }
 async void IntroFinished(){
  bool go=introWatching&&finalView&&Visible&&!busy&&!partyBusy&&!botsPanel.IsBusy;introWatching=false;RefreshIntroControls();
  if(go)await StartPrepared();
 }
}
}
