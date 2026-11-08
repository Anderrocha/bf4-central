using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace BF4CentralSetup {
 static class Setup {
  static readonly string[] Payload={"BF4 Central.exe","LEIA-ME.txt"};
  static readonly string Destination=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","BF4 Central");
  static string Hash(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","");}
  static byte[] Read(ZipArchiveEntry entry,int limit){if(entry==null||entry.Length<1||entry.Length>limit)throw new InvalidDataException("Pacote de instalação incompleto ou inválido.");using(var stream=entry.Open())using(var buffer=new MemoryStream()){stream.CopyTo(buffer);if(buffer.Length!=entry.Length)throw new InvalidDataException("Tamanho de arquivo divergente.");return buffer.ToArray();}}
  static byte[][] Verify(){using(var stream=typeof(Setup).Assembly.GetManifestResourceStream("BF4Central.Payload"))using(var archive=new ZipArchive(stream,ZipArchiveMode.Read)){if(archive.Entries.Count!=3)throw new InvalidDataException("Arquivos inesperados no instalador.");var hashes=Encoding.UTF8.GetString(Read(archive.GetEntry("SHA256.txt"),4096)).Trim().Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries);if(hashes.Length!=Payload.Length)throw new InvalidDataException("Inventário inválido.");var data=new byte[Payload.Length][];for(int i=0;i<Payload.Length;i++){data[i]=Read(archive.GetEntry(Payload[i]),32*1024*1024);if(!String.Equals(hashes[i],Hash(data[i])+"  "+Payload[i],StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("SHA-256 divergente: "+Payload[i]);}return data;}}
  static string Install(string root,bool shortcut){var data=Verify();Directory.CreateDirectory(root);if((File.GetAttributes(root)&FileAttributes.ReparsePoint)!=0)throw new IOException("A pasta de destino não pode ser um link de diretório.");var old=new byte[Payload.Length][];for(int i=0;i<Payload.Length;i++){var file=Path.Combine(root,Payload[i]);if(File.Exists(file)){if((File.GetAttributes(file)&FileAttributes.ReparsePoint)!=0)throw new IOException("O destino contém um link de arquivo.");old[i]=File.ReadAllBytes(file);}}
   if(old.Any(b=>b!=null)){var backup=Path.Combine(root,"backup-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,5));Directory.CreateDirectory(backup);for(int i=0;i<Payload.Length;i++)if(old[i]!=null)File.WriteAllBytes(Path.Combine(backup,Payload[i]),old[i]);}
   int done=0;try{for(int i=0;i<Payload.Length;i++){var file=Path.Combine(root,Payload[i]);var tmp=file+"."+Guid.NewGuid().ToString("N")+".tmp";try{File.WriteAllBytes(tmp,data[i]);if(File.Exists(file))File.Replace(tmp,file,null);else File.Move(tmp,file);done++;if(Hash(File.ReadAllBytes(file))!=Hash(data[i]))throw new IOException("Falha na verificação: "+Payload[i]);}finally{if(File.Exists(tmp))File.Delete(tmp);}}}catch{for(int i=0;i<done;i++){var file=Path.Combine(root,Payload[i]);if(old[i]==null)File.Delete(file);else File.WriteAllBytes(file,old[i]);}throw;}
   if(shortcut){try{var type=Type.GetTypeFromProgID("WScript.Shell");dynamic shell=Activator.CreateInstance(type);string start=Environment.GetFolderPath(Environment.SpecialFolder.Programs);dynamic link=shell.CreateShortcut(Path.Combine(start,"BF4 Central.lnk"));link.TargetPath=Path.Combine(root,Payload[0]);link.WorkingDirectory=root;link.Description="Central local do Battlefield 4";link.Save();}catch{return "Central instalada em "+root+". Não foi possível criar o atalho no menu Iniciar.";}}
   return "Central instalada em "+root+". Abra BF4 Central pelo menu Iniciar.";
  }
  [STAThread] static int Main(string[] args){if(args.Length==2&&args[0]=="--self-test"){try{var root=Path.Combine(Path.GetTempPath(),"BF4Central-setup-test-"+Guid.NewGuid().ToString("N"));Install(root,false);Install(root,false);if(!Directory.EnumerateDirectories(root,"backup-*").Any())throw new IOException("Backup de atualização ausente.");File.WriteAllText(args[1],"PASS pacote, hashes, instalação isolada e backup da atualização. "+root);return 0;}catch(Exception ex){File.WriteAllText(args[1],"FAIL "+ex);return 1;}}
   Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);using(var form=new Form{Text="Instalar BF4 Central",ClientSize=new Size(630,345),StartPosition=FormStartPosition.CenterScreen,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,Font=new Font("Segoe UI",10),BackColor=Color.FromArgb(232,237,240),ForeColor=Color.FromArgb(30,52,66)}){
    var title=new Label{Text="BF4 / Instalar Central",Left=24,Top=22,Width=570,Height=45,Font=new Font("Bahnschrift",23,FontStyle.Bold)};form.Controls.Add(title);form.Controls.Add(new Label{Text="Instala a Central e cria um atalho no menu Iniciar.\r\nO Battlefield 4 e o mod devem estar instalados separadamente.\r\n\r\nDestino: "+Destination,Left=26,Top=78,Width=574,Height=100});
    var state=new Label{Left=26,Top=191,Width=574,Height=70,Text="Pronto para instalar. Arquivos anteriores da Central recebem backup.",AccessibleName="Estado da instalação"};form.Controls.Add(state);var install=new Button{Text="Instalar Central",Left=26,Top=272,Width=190,Height=40,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(30,52,66),ForeColor=Color.White};form.Controls.Add(install);var close=new Button{Text="Fechar",Left=224,Top=272,Width=125,Height=40};form.Controls.Add(close);close.Click+=(s,e)=>form.Close();bool busy=false;form.FormClosing+=(s,e)=>e.Cancel=busy;install.Click+=async(s,e)=>{busy=true;install.Enabled=close.Enabled=false;state.Text="Verificando e instalando…";try{state.Text=await Task.Run(()=>Install(Destination,true));}catch(Exception ex){state.Text="Não foi possível instalar: "+ex.Message;install.Enabled=true;}finally{busy=false;close.Enabled=true;}};form.AcceptButton=install;form.CancelButton=close;Application.Run(form);return 0;}
  }
 }
}
