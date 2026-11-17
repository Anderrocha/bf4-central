using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
namespace BF4Bots {
// Música de abertura de cada mapa, extraída do próprio jogo deste PC para
// %LOCALAPPDATA%\BF4BotLauncher\music\<IdDoMapa>.mp3 (não vai para o GitHub: é conteúdo da EA).
public static class MapMusic {
 [DllImport("winmm.dll",CharSet=CharSet.Unicode)]static extern int mciSendString(string command,StringBuilder result,int length,IntPtr callback);
 const string Alias="bf4mapmusic";
 static string current;
 public static string Folder{get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BF4BotLauncher","music");}}
 static string SettingFile{get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BF4BotLauncher","map-music.txt");}}
 public static bool Enabled{
  get{try{return !File.Exists(SettingFile)||File.ReadAllText(SettingFile).Trim()!="0";}catch{return true;}}
  set{try{Directory.CreateDirectory(Path.GetDirectoryName(SettingFile));File.WriteAllText(SettingFile,value?"1":"0");}catch(IOException){}catch(UnauthorizedAccessException){}if(!value)Stop();}
 }
 public static string FileFor(string map){
  if(String.IsNullOrEmpty(map)||map.IndexOfAny(Path.GetInvalidFileNameChars())>=0||map.Contains("\""))return null;
  string file=Path.Combine(Folder,map+".mp3");return File.Exists(file)?file:null;
 }
 public static bool Available{get{try{return Directory.Exists(Folder)&&Directory.GetFiles(Folder,"*.mp3").Length>0;}catch{return false;}}}
 // Troca para a música do mapa escolhido; mapa sem música para a anterior.
 public static void Play(string map){
  if(!Enabled){Stop();return;}
  if(map==current)return;
  Stop();string file=FileFor(map);if(file==null)return;
  if(mciSendString("open \""+file+"\" type mpegvideo alias "+Alias,null,0,IntPtr.Zero)!=0)return;
  mciSendString("setaudio "+Alias+" volume to 700",null,0,IntPtr.Zero);
  if(mciSendString("play "+Alias,null,0,IntPtr.Zero)!=0){mciSendString("close "+Alias,null,0,IntPtr.Zero);return;}
  current=map;
 }
 public static void Stop(){if(current==null)return;mciSendString("stop "+Alias,null,0,IntPtr.Zero);mciSendString("close "+Alias,null,0,IntPtr.Zero);current=null;}
 public static string Playing{get{return current;}}
}
public sealed partial class Central {
 TabPage serverTab;
 bool OnServerTab{get{return serverTab!=null&&tabs!=null&&tabs.SelectedTab==serverTab&&Visible&&!finalView;}}
 void MusicFor(RotationEntry choice){if(choice!=null&&OnServerTab)MapMusic.Play(choice.Map);}
 CheckBox MusicSwitch(){
  var box=new CheckBox{Text="Música do mapa",Checked=MapMusic.Enabled,AutoSize=true,Padding=new Padding(15,6,0,0),AccessibleName="Tocar a música de abertura do mapa escolhido",Enabled=MapMusic.Available};
  box.CheckedChanged+=(s,e)=>{MapMusic.Enabled=box.Checked;if(box.Checked)MusicFor(selectedChoice);};
  tabs.SelectedIndexChanged+=(s,e)=>{if(OnServerTab)MusicFor(selectedChoice);else MapMusic.Stop();};
  FormClosed+=(s,e)=>MapMusic.Stop();
  return box;
 }
}
}
