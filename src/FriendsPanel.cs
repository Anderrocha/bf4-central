using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BF4Bots {
public sealed partial class Central {
 ComboBox lanAddress;TextBox leaderName,guestNames,joinName;Label partyStatus,inviteSummary;CheckBox keepParty;LanInvite incoming;bool partyBusy;int partyErrors;readonly Timer partyTimer=new Timer{Interval=10000};
 void BuildFriends(){var page=new TabPage("Amigos • rede local"){BackColor=Theme.Background,AutoScroll=true};tabs.TabPages.Add(page);var layout=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,Padding=new Padding(24),ColumnCount=2};layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,210));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));page.Controls.Add(layout);
  Action<Control,int> wide=(control,row)=>{layout.Controls.Add(control,0,row);layout.SetColumnSpan(control,2);};
  wide(new Label{Text="Seu pelotão, na mesma rede",Font=Theme.Heading,AutoSize=true,Margin=new Padding(0,0,0,12)},0);
  wide(new Label{Text="Anfitrião: prepare a rede com o BF4 fechado. Depois envie o convite e esta Central ao amigo.\r\nCada computador precisa do jogo/mod compatível instalado e de um nome de jogador diferente.",AutoSize=true,ForeColor=Theme.Muted,Margin=new Padding(0,0,0,16)},1);
  layout.Controls.Add(new Label{Text="IP deste computador",AutoSize=true,Padding=new Padding(0,8,0,0)},0,2);lanAddress=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Dock=DockStyle.Fill,AccessibleName="IP da rede local",Margin=new Padding(8)};lanAddress.Items.AddRange(Friends.Addresses());if(lanAddress.Items.Count>0)lanAddress.SelectedIndex=0;layout.Controls.Add(lanAddress,1,2);
  var hostButtons=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill};hostButtons.Controls.Add(AsyncButton("Preparar rede local",PrepareLan,true));hostButtons.Controls.Add(Button("Salvar convite…",ExportInvite));hostButtons.Controls.Add(Button("Atualizar IPs",()=>{lanAddress.Items.Clear();lanAddress.Items.AddRange(Friends.Addresses());if(lanAddress.Items.Count>0)lanAddress.SelectedIndex=0;}));hostButtons.Controls.Add(Button("Configurar rede no Windows",()=>System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:network-ethernet"){UseShellExecute=true})));wide(hostButtons,3);
  leaderName=Field(layout,4,"Seu nome na partida");leaderName.Text=user.Text;guestNames=Field(layout,5,"Nomes dos amigos");guestNames.AccessibleDescription="Até quatro nomes exatos separados por ponto e vírgula";
  wide(new Label{Text="Separe até 4 amigos por ponto e vírgula. O grupo segue o time e o pelotão do anfitrião.\r\nSe estiver sem pelotão, será usado um vazio. Mudanças aguardam o renascimento; não matam o jogador.",AutoSize=true,ForeColor=Theme.Muted,Margin=new Padding(0,8,0,8)},6);
  keepParty=new CheckBox{Text="Manter o grupo reunido enquanto esta Central estiver aberta",AutoSize=true,AccessibleName="Manter mesmo pelotão"};keepParty.CheckedChanged+=(s,e)=>{partyErrors=0;partyTimer.Enabled=keepParty.Checked;leaderName.Enabled=guestNames.Enabled=!keepParty.Checked;partyStatus.Text=keepParty.Checked?"Monitoramento ativo. Verificando entrada, time e pelotão…":"Monitoramento parado. Nenhum jogador será movido.";};wide(keepParty,7);
  var partyButtons=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true};partyButtons.Controls.Add(AsyncButton("Reunir / verificar agora",PartyTick));wide(partyButtons,8);partyStatus=new Label{Text="Informe os nomes e ative Manter o grupo reunido no computador anfitrião.",AutoSize=true,MaximumSize=new Size(850,0),ForeColor=Theme.Muted,Margin=new Padding(0,8,0,18)};wide(partyStatus,9);
  wide(new Label{Text="Recebi um convite",Font=Theme.Heading,AutoSize=true,Margin=new Padding(0,8,0,8)},10);joinName=Field(layout,11,"Meu nome para entrar");joinName.Text=user.Text;var joinButtons=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true};joinButtons.Controls.Add(Button("Abrir convite…",ImportInvite));joinButtons.Controls.Add(AsyncButton("Entrar no servidor do amigo",JoinFriend,true));joinButtons.Controls.Add(AsyncButton("Voltar ao meu servidor",ReturnToLocal));wide(joinButtons,12);inviteSummary=new Label{Text="Abra o arquivo .bf4invite enviado pelo anfitrião. O endereço e o nome dele aparecerão aqui antes de entrar.",AutoSize=true,MaximumSize=new Size(850,0),ForeColor=Theme.Muted,Margin=new Padding(0,10,0,10)};wide(inviteSummary,13);
  wide(new Label{Text="No BF4, abra Multiplayer → Servidores e entre no servidor do anfitrião.\r\nA rede do Windows deve estar como Privada. O convite não inclui senha administrativa.\r\nSe o Wi-Fi isolar dispositivos, use cabo ou desative o isolamento na rede do roteador.",AutoSize=true,ForeColor=Theme.Muted,Margin=new Padding(0,8,0,8)},14);
  partyTimer.Tick+=async(s,e)=>{if(keepParty.Checked)await PartyTick();};
 }
 async Task PrepareLan(){string ip=Convert.ToString(lanAddress.SelectedItem);if(!botsPanel.Confirm("Preparar partida na rede local","Backend e servidor serão reiniciados e a partida será interrompida. Acesso será permitido apenas na rede Privada e na sub-rede local. Feche o BF4 antes de continuar.","Preparar rede local"))return;await Work("Preparando o anfitrião…",async()=>{await Task.Run(()=>Friends.Host(root,ip,Progress()));LoadSettings();Status("Rede local preparada em "+ip+". Salve o convite, abra o jogo e mantenha esta Central aberta para reunir o pelotão.");});}
 void ExportInvite(){string ip=Convert.ToString(lanAddress.SelectedItem);var config=Settings.Read(Path.Combine(root,"Server"));if(!Friends.Addresses().Contains(ip)||Settings.Get(config,"ServerIP","")!=ip)throw new InvalidOperationException("Clique em Preparar rede local antes de criar o convite.");if(Settings.Get(config,"ClientPort","10040")!="10040")throw new InvalidOperationException("Porta do backend incompatível com este convite.");var invite=new LanInvite{Version=1,Host=ip,Leader=leaderName.Text.Trim(),ExpiresUtc=DateTime.UtcNow.AddHours(24)};string text=invite.Encode();using(var dialog=new SaveFileDialog{Filter="Convite BF4|*.bf4invite",FileName="Partida BF4.bf4invite",DefaultExt="bf4invite"}){if(dialog.ShowDialog(this)!=DialogResult.OK)return;File.WriteAllText(dialog.FileName,text,new System.Text.UTF8Encoding(false));Status("Convite salvo por 24 horas. Envie o arquivo e a Central ao amigo. O convite configura a conexão; não é uma senha de acesso.");}}
 void ImportInvite(){using(var dialog=new OpenFileDialog{Filter="Convite BF4|*.bf4invite",CheckFileExists=true}){if(dialog.ShowDialog(this)!=DialogResult.OK)return;try{if(new FileInfo(dialog.FileName).Length>4096)throw new InvalidDataException("Convite excessivo.");var parsed=LanInvite.Decode(File.ReadAllText(dialog.FileName));incoming=parsed;inviteSummary.Text="Anfitrião: "+parsed.Leader+" • endereço: "+parsed.Host+" • válido até "+parsed.ExpiresUtc.ToLocalTime().ToString("g")+"\r\nConfira o anfitrião e clique em Aceitar e abrir o jogo. Feche o BF4 atual primeiro.";}catch{incoming=null;inviteSummary.Text="Convite inválido. Peça um novo arquivo ao anfitrião.";throw;}}}
 bool ConfirmGameSwitch() {
  return !OneClick.IsGameOpen(root)||botsPanel.Confirm("Trocar de servidor","A Central vai fechar o Battlefield 4 e abri-lo com a nova conexão.","Trocar de servidor");
 }
 async Task ReturnToLocal() {
  if(!ConfirmGameSwitch())return;
  bool switched=false;
  await Work("Voltando ao servidor deste PC…",async()=>{
   await Task.Run(()=>Friends.ReturnLocal(root));
   LoadSettings();
   incoming=null;
   inviteSummary.Text="Conexão local selecionada. Abra um convite para entrar no servidor de um amigo.";
   switched=true;
  });
  if(switched)await StartAll();
 }
 async Task JoinFriend(){if(incoming==null)throw new InvalidOperationException("Abra um convite válido primeiro.");if(!ConfirmGameSwitch())return;var invite=incoming;string name=joinName.Text.Trim();bool accepted=false;await Work("Verificando o computador anfitrião…",async()=>{await Task.Run(()=>Friends.Accept(root,invite,name));LoadSettings();accepted=true;Status("Conexão configurada para "+invite.Leader+". Abrindo o jogo…");});if(accepted)await StartAll();}
 async Task PartyTick(){if(partyBusy||busy||botsPanel.IsBusy)return;string leader=leaderName.Text.Trim();var guests=guestNames.Text.Split(new[]{';'},StringSplitOptions.RemoveEmptyEntries).Select(n=>n.Trim()).ToArray();var c=Settings.Read(root);if(Settings.Get(c,"BlazeIP","127.0.0.1")!="127.0.0.1"){keepParty.Checked=false;partyStatus.Text="O controle do pelotão deve ficar no computador anfitrião.";return;}partyBusy=true;try{string result=await Task.Run(()=>{using(var connection=new Rcon()){connection.Connect("127.0.0.1",Int32.Parse(Settings.Get(c,"RconPort","25210")),Settings.Get(c,"RconPassword",""));return Friends.Reconcile(words=>connection.Command(words),leader,guests);}});partyErrors=0;partyStatus.Text=result;}catch(Exception ex){partyErrors++;partyStatus.Text="Não foi possível reunir: "+ex.Message;if(partyErrors>=3){keepParty.Checked=false;partyStatus.Text="Monitoramento pausado após três falhas. Confira o servidor e os nomes; ative novamente. "+ex.Message;}}finally{partyBusy=false;}}
}
}
