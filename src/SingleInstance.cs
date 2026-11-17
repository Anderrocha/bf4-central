using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
namespace BF4Bots {
// Só uma Central por instalação do jogo. Antes, se outra Central (ex.: versão antiga em outra pasta)
// estivesse aberta, esta fechava em silêncio e a pessoa continuava vendo a antiga.
public static class SingleInstance {
 [DllImport("user32.dll")]static extern bool PostMessage(IntPtr window,uint message,IntPtr w,IntPtr l);
 // true: pode abrir esta (a outra foi fechada e o bloqueio foi obtido). false: encerrar esta.
 public static bool Resolve(string[] args,Mutex single){
  string mine=Application.ExecutablePath;int me=Process.GetCurrentProcess().Id;
  var others=Process.GetProcesses().Where(p=>p.Id!=me&&p.ProcessName.StartsWith("BF4 Central",StringComparison.OrdinalIgnoreCase)&&!p.ProcessName.EndsWith(".Tests",StringComparison.OrdinalIgnoreCase)).ToList();
  foreach(var p in others){
   try{if(OneClick.SamePath(p.MainModule.FileName,mine)){OneClick.Focus(p);if(args.Contains("--play"))PostMessage(p.MainWindowHandle,0x8031,IntPtr.Zero,IntPtr.Zero);return false;}}catch{}
  }
  string title=L.T("BF4 Central já está aberta");
  var other=others.FirstOrDefault();
  if(other==null){MessageBox.Show(L.T("Outra BF4 Central está aberta para esta instalação do jogo. Feche-a e abra de novo."),title,MessageBoxButtons.OK,MessageBoxIcon.Information);return false;}
  string path=null,version=null;try{path=other.MainModule.FileName;version=FileVersionInfo.GetVersionInfo(path).FileVersion;}catch{}
  string current=FileVersionInfo.GetVersionInfo(mine).FileVersion;
  string text=L.T("Já existe outra BF4 Central aberta")+(version!=null?" ("+L.T("versão")+" "+version+")":"")+(path!=null?":\n"+path:".")+"\n\n"+L.T("Fechar essa e abrir esta")+" ("+L.T("versão")+" "+current+")?";
  if(MessageBox.Show(text,title,MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes){try{OneClick.Focus(other);}catch{}return false;}
  bool closed=false;try{closed=other.CloseMainWindow()&&other.WaitForExit(15000);}catch{}
  if(!closed){MessageBox.Show(L.T("Não consegui fechar a outra Central. Feche-a pela própria janela e abra esta de novo."),title,MessageBoxButtons.OK,MessageBoxIcon.Warning);return false;}
  try{if(!single.WaitOne(10000)){MessageBox.Show(L.T("Outra BF4 Central está aberta para esta instalação do jogo. Feche-a e abra de novo."),title,MessageBoxButtons.OK,MessageBoxIcon.Information);return false;}}catch(AbandonedMutexException){}
  return true;
 }
}
}
