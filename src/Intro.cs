using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace BF4Bots {
// Intro do BF4 no banner da tela final. Quadros e som ficam em %LOCALAPPDATA%\BF4BotLauncher\intro
// (intro.bin + intro.mp3), gerados neste PC a partir do vídeo do usuário; não vão para o GitHub.
// intro.bin: "BF4I", fps (int32), quantidade (int32) e, para cada quadro, tamanho (int32) + JPEG.
public sealed class IntroClip {
 public readonly int Fps,Count;readonly byte[][] frames;
 IntroClip(int fps,byte[][] frames){Fps=fps;this.frames=frames;Count=frames.Length;}
 public static string Folder{get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BF4BotLauncher","intro");}}
 static IntroClip cached;static bool loaded;
 public static IntroClip Load(){if(loaded)return cached;loaded=true;try{string f=Path.Combine(Folder,"intro.bin");if(File.Exists(f)&&new FileInfo(f).Length<200*1024*1024)using(var s=File.OpenRead(f))cached=Read(s);}catch{cached=null;}return cached;}
 public static IntroClip Read(Stream s){var r=new BinaryReader(s);if(new string(r.ReadChars(4))!="BF4I")return null;int fps=r.ReadInt32(),count=r.ReadInt32();if(fps<1||fps>60||count<1||count>3000)return null;var list=new byte[count][];for(int i=0;i<count;i++){int n=r.ReadInt32();if(n<1||n>4000000)return null;list[i]=r.ReadBytes(n);if(list[i].Length!=n)return null;}return new IntroClip(fps,list);}
 public Image Frame(int i){if(i<0||i>=Count)return null;try{using(var m=new MemoryStream(frames[i]))using(var img=Image.FromStream(m))return new Bitmap(img);}catch{return null;}}
}
// Toca a intro uma vez, com som, e para no último quadro (a arte do Battlefield 4).
public sealed class IntroPlayer : IDisposable {
 [DllImport("winmm.dll",CharSet=CharSet.Unicode)]static extern int mciSendString(string command,System.Text.StringBuilder result,int length,IntPtr callback);
 const string Alias="bf4intro";
 readonly BannerPanel banner;Timer timer;Stopwatch clock;IntroClip clip;int shown=-1;bool sound;
 public IntroPlayer(BannerPanel banner){this.banner=banner;}
 public static bool Available{get{return IntroClip.Load()!=null;}}
 public void Play(bool withSound){
  Stop();clip=IntroClip.Load();if(clip==null)return;
  string audio=Path.Combine(IntroClip.Folder,"intro.mp3");
  if(withSound&&File.Exists(audio)&&!audio.Contains("\"")&&mciSendString("open \""+audio+"\" type mpegvideo alias "+Alias,null,0,IntPtr.Zero)==0){mciSendString("setaudio "+Alias+" volume to 700",null,0,IntPtr.Zero);sound=mciSendString("play "+Alias,null,0,IntPtr.Zero)==0;if(!sound)mciSendString("close "+Alias,null,0,IntPtr.Zero);}
  clock=Stopwatch.StartNew();shown=-1;ShowAt(0);
  timer=new Timer{Interval=15};timer.Tick+=(s,e)=>{int i=(int)(clock.ElapsedMilliseconds*clip.Fps/1000);if(i>=clip.Count-1){ShowAt(clip.Count-1);timer.Stop();return;}ShowAt(i);};timer.Start();
 }
 void ShowAt(int i){if(i==shown||banner.IsDisposed)return;var img=clip.Frame(i);if(img==null)return;shown=i;banner.Frame=img;}
 // Para o som; keepLast mantém o quadro atual, senão volta à imagem fixa.
 public void Stop(bool keepLast=false){if(timer!=null){timer.Dispose();timer=null;}if(sound){mciSendString("stop "+Alias,null,0,IntPtr.Zero);mciSendString("close "+Alias,null,0,IntPtr.Zero);sound=false;}if(!keepLast&&!banner.IsDisposed)banner.Frame=null;}
 public void Dispose(){Stop(true);}
}
public sealed partial class Central {
 BannerPanel bannerPanel;IntroPlayer intro;
 void IntroOnFinal(bool show){
  if(!EnsureIntro())return;
  // Um som por vez: a tela final cala a música do mapa; ao voltar, a intro para e a música do mapa volta.
  if(show){MapMusic.Stop();if(IntroPlayer.Available)intro.Play(MapMusic.Enabled);}
  else{intro.Stop();MusicFor(selectedChoice);}
 }
 bool EnsureIntro(){if(bannerPanel==null)return false;if(intro==null){intro=new IntroPlayer(bannerPanel);FormClosed+=(s,e)=>intro.Dispose();}return true;}
 // Mesma intro ao abrir a Central (pedido de 19/11). Ao entrar na aba Servidor ela para e a música do mapa assume.
 void IntroOnStart(){
  if(finalView||!EnsureIntro()||!IntroPlayer.Available)return;
  MapMusic.Stop();intro.Play(MapMusic.Enabled);
  tabs.SelectedIndexChanged+=(s,e)=>{if(OnServerTab&&!finalView)intro.Stop();};
 }
}
}
