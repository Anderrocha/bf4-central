using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;
namespace BF4Bots {
// Liberação da Central: a pessoa entra com a conta do Sócio Premiado (telefone + senha);
// o site confere se há compra paga via Pix e devolve uma licença assinada, presa a este PC.
// A licença fica salva e vale sem internet. Só "Iniciar tudo e jogar" depende dela.
public static class License {
 public const string Endpoint="https://sociopremiado.com.br/central-licenca.php";
 // Chave pública RSA-2048 do site (a privada fica fora do public_html no servidor).
 const string Modulus="o3uKe6hUPwdsitxS7EHZZVGOsRmy1Rla0TDrpgMuQG1xdSnvQRpbwbbgwYm7piQ4PD2Uurg7efcQvjpJuiI89+eyVPS9RPSTx8eV+4OBOW/kJdF9fvPuTupFklBmrA1kMyKYl2f9FNJFpKgDt6PrxEhOXUrCLJOZT9u360/pE3xdvdgqbn64z9txmoTJr2xaAMN/UB2pWgYK70g/W1fg+UTuP/+ZL/zUN13677B68vs1qXH6JYuf4DdxpT89Q0prWzMqUwIShii65aZzr6r0UlkgNtKkF1WEkgmyMDMe+XmxrTLebzWEDLVyt1bOZ24T6Iuexo/EdZmyb5TNjNaMrw==",Exponent="AQAB";
 static string FilePath{get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BF4BotLauncher","licenca.txt");}}
 static string cachedName;static bool? cachedValid;
 public static event Action Changed;
 public static bool Valid{get{if(cachedValid==null){string name;cachedValid=Check(Read(),MachineId(),out name);cachedName=name;}return cachedValid.Value;}}
 public static string Name{get{return Valid?cachedName:null;}}
 static string Read(){try{return File.Exists(FilePath)?File.ReadAllText(FilePath).Trim():null;}catch{return null;}}
 // Identificador deste Windows (MachineGuid); reserva: nome do PC + usuário.
 public static string MachineId(){
  try{using(var k=RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,RegistryView.Registry64).OpenSubKey(@"SOFTWARE\Microsoft\Cryptography")){var v=k==null?null:k.GetValue("MachineGuid") as string;if(!String.IsNullOrEmpty(v))return v.Trim();}}catch{}
  using(var sha=SHA256.Create())return "PC-"+BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(Environment.MachineName+"|"+Environment.UserName))).Replace("-","").Substring(0,32);
 }
 // Licença = base64(carga) + "." + base64(assinatura). Carga: BF4C1|máquina|conta|nome|data.
 public static bool Check(string license,string machine,out string name){
  name=null;if(String.IsNullOrEmpty(license))return false;
  try{
   int dot=license.IndexOf('.');if(dot<1)return false;
   byte[] payload=Convert.FromBase64String(license.Substring(0,dot)),signature=Convert.FromBase64String(license.Substring(dot+1));
   using(var rsa=new RSACryptoServiceProvider()){rsa.PersistKeyInCsp=false;rsa.ImportParameters(new RSAParameters{Modulus=Convert.FromBase64String(Modulus),Exponent=Convert.FromBase64String(Exponent)});
    if(!rsa.VerifyData(payload,CryptoConfig.MapNameToOID("SHA256"),signature))return false;}
   var parts=Encoding.UTF8.GetString(payload).Split('|');
   if(parts.Length<5||parts[0]!="BF4C1"||!String.Equals(parts[1],machine,StringComparison.OrdinalIgnoreCase))return false;
   name=parts[3];return true;
  }catch{return false;}
 }
 public sealed class Result{public bool Ok;public string Message,Name;}
 // Pergunta ao site. Não guarda telefone nem senha: só a licença devolvida.
 public static Result Activate(string phone,string password){
  try{
   ServicePointManager.SecurityProtocol|=(SecurityProtocolType)3072;
   string body;
   using(var web=new WebClient()){web.Encoding=Encoding.UTF8;web.Headers[HttpRequestHeader.UserAgent]="BF4Central";
    try{body=Encoding.UTF8.GetString(web.UploadValues(Endpoint,"POST",new NameValueCollection{{"phone",phone??""},{"password",password??""},{"machine",MachineId()}}));}
    catch(WebException ex){var r=ex.Response as HttpWebResponse;if(r==null)throw;using(var s=new StreamReader(r.GetResponseStream(),Encoding.UTF8))body=s.ReadToEnd();}}
   return Accept(body);
  }catch(Exception ex){return new Result{Message="Não foi possível falar com o site: "+ex.Message};}
 }
 internal static Result Accept(string body){
  var json=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(body??"");
  Func<string,string> get=k=>{object v;return json!=null&&json.TryGetValue(k,out v)&&v!=null?v.ToString():null;};
  if(get("ok")=="True"){
   string lic=get("licenca"),name;
   if(!Check(lic,MachineId(),out name))return new Result{Message="O site respondeu, mas a liberação não confere com este PC. Tente de novo."};
   Directory.CreateDirectory(Path.GetDirectoryName(FilePath));File.WriteAllText(FilePath,lic);
   cachedValid=true;cachedName=name;var h=Changed;if(h!=null)h();
   return new Result{Ok=true,Name=name,Message="Central liberada"+(String.IsNullOrEmpty(name)?"":" para "+name)+". Bom jogo!"};
  }
  string why=get("motivo"),msg=get("msg")??"Resposta inesperada do site.";
  if(why=="semcompra")msg="Conta encontrada"+(String.IsNullOrEmpty(get("nome"))?"":" ("+get("nome")+")")+", mas ainda não há compra paga via Pix. Faça uma compra de qualquer valor no site e clique em Verificar de novo.";
  return new Result{Message=msg};
 }
 public static void Forget(){try{File.Delete(FilePath);}catch{}cachedValid=false;cachedName=null;var h=Changed;if(h!=null)h();}
 public static void OpenSite(){try{System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(PromoCard.Url){UseShellExecute=true});}catch{}}
 // Mostra a janela de entrada quando ainda não há licença. Devolve true se liberado.
 public static bool Ensure(IWin32Window owner){if(Valid)return true;using(var d=new LicenseDialog())d.ShowDialog(owner);return Valid;}
}
// Janela "Entrar com a conta do Sócio Premiado".
public sealed class LicenseDialog : Form {
 readonly TextBox phone,password;readonly Label status;readonly ActionButton verify;
 public LicenseDialog(){
  Text=L.T("Liberar a Central");FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;ShowInTaskbar=false;StartPosition=FormStartPosition.CenterParent;BackColor=Theme.Background;Font=Theme.Body;ClientSize=new Size(520,360);
  var grid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=7,Padding=new Padding(24,18,24,16)};
  grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,120));grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
  foreach(int h in new[]{40,64,40,40,30,64,46})grid.RowStyles.Add(new RowStyle(SizeType.Absolute,h));
  var title=new Label{Text=L.T("Entre com sua conta do Sócio Premiado"),Font=new Font("Bahnschrift",14,FontStyle.Bold),ForeColor=Theme.Ink,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft};grid.Controls.Add(title,0,0);grid.SetColumnSpan(title,2);
  var hint=new Label{Text=L.T("Use o mesmo telefone e senha do site. Para liberar, a conta precisa de uma compra paga via Pix, de qualquer valor."),ForeColor=Theme.Muted,Dock=DockStyle.Fill};grid.Controls.Add(hint,0,1);grid.SetColumnSpan(hint,2);
  grid.Controls.Add(new Label{Text=L.T("Telefone"),Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,2);phone=new TextBox{Dock=DockStyle.Fill,Margin=new Padding(0,8,0,0),AccessibleName="Telefone"};grid.Controls.Add(phone,1,2);
  grid.Controls.Add(new Label{Text=L.T("Senha"),Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,3);password=new TextBox{Dock=DockStyle.Fill,UseSystemPasswordChar=true,Margin=new Padding(0,8,0,0),AccessibleName="Senha"};grid.Controls.Add(password,1,3);
  var site=new LinkLabel{Text=L.T("Ainda não comprou? Abrir sociopremiado.com.br"),Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,LinkBehavior=LinkBehavior.HoverUnderline};site.LinkClicked+=(s,e)=>License.OpenSite();grid.Controls.Add(site,0,4);grid.SetColumnSpan(site,2);
  status=new Label{Dock=DockStyle.Fill,ForeColor=Theme.Muted,AccessibleName="Resultado da verificação"};grid.Controls.Add(status,0,5);grid.SetColumnSpan(status,2);
  var row=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft,Margin=Padding.Empty};
  verify=new ActionButton{Text=L.T("Verificar e liberar"),BackColor=Theme.Ink,ForeColor=Color.White,Width=190,Height=38};var close=new ActionButton{Text=L.T("Fechar"),BackColor=Theme.Surface,ForeColor=Theme.Ink,Width=110,Height=38};
  close.Click+=(s,e)=>Close();verify.Click+=async(s,e)=>await Verify();row.Controls.Add(verify);row.Controls.Add(close);grid.Controls.Add(row,0,6);grid.SetColumnSpan(row,2);
  AcceptButton=verify;CancelButton=close;Controls.Add(grid);
 }
 async Task Verify(){
  if(phone.Text.Trim().Length<8||password.Text.Length==0){status.ForeColor=Theme.Accent;status.Text=L.T("Informe telefone e senha.");return;}
  verify.Enabled=false;status.ForeColor=Theme.Muted;status.Text=L.T("Verificando no site…");
  string p=phone.Text,s=password.Text;var r=await Task.Run(()=>License.Activate(p,s));
  if(IsDisposed)return;verify.Enabled=true;status.ForeColor=r.Ok?ColorTranslator.FromHtml("#0B7A43"):Theme.Accent;status.Text=L.T(r.Message);
  if(r.Ok){password.Clear();var t=new Timer{Interval=1400};t.Tick+=(a,b)=>{t.Stop();t.Dispose();if(!IsDisposed)Close();};t.Start();}
 }
}
}
