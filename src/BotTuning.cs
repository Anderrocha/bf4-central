using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
// Ajustes finos dos bots por intensidade (Desligado/Baixa/Média/Alta ou Não/Sim).
// Cada nível é um conjunto de valores do Server\dinput8_bf4hook.ini; só chaves que já existem no arquivo.
namespace BF4Bots {
public sealed class Tuning {
 public string Title,Hint;public string[] Levels;public Dictionary<string,string>[] Values;
 public Tuning(string title,string hint,string[] levels,string[] keys,params string[][] values){
  Title=title;Hint=hint;Levels=levels;if(values.Length!=levels.Length)throw new InvalidDataException("Ajuste inválido: "+title);
  Values=values.Select(v=>{if(v.Length!=keys.Length)throw new InvalidDataException("Ajuste inválido: "+title);var d=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);for(int i=0;i<keys.Length;i++)d[keys[i]]=v[i];return d;}).ToArray();
 }
}
public static class BotTuning {
 static readonly string[] YN={"Não","Sim"},L4={"Desligado","Baixa","Média","Alta"},L3={"Baixa","Média","Alta"};
 public static readonly Tuning[] All={
  new Tuning("Combate dos bots","Não = os bots não miram nem atiram (bom para treinar movimentação).",YN,new[]{"EnableAim","EnableFire"},new[]{"0","0"},new[]{"1","1"}),
  new Tuning("Bots atiram em outros bots","Não = os bots só enfrentam jogadores de verdade.",YN,new[]{"AllowBotTargets"},new[]{"0"},new[]{"1"}),
  new Tuning("Munição","Desligado = munição normal. Baixa/Média/Alta = infinita, com reserva mínima de 100, 400 ou 1000 balas.",L4,new[]{"BotInfiniteReserve","BotReserveFloor"},new[]{"0","1000"},new[]{"1","100"},new[]{"1","400"},new[]{"1","1000"}),
  new Tuning("Caixas de munição e apoio","Bots largam apoio perto dos combates. Intensidade = intervalo entre caixas (15 s, 9 s, 5 s).",L4,new[]{"BotHotspotSupplies","BotSupplyCooldownMs"},new[]{"0","5000"},new[]{"1","15000"},new[]{"1","9000"},new[]{"1","5000"}),
  new Tuning("Granadas","Quantidade e frequência. Alta = infinitas, a cada 3 a 6 s, e limpam bandeiras com granadas.",L4,new[]{"BotInfiniteGrenades","BotGrenadeReserve","BotClusterGrenades","BotGrenadeClearObjectives","BotGrenadeCooldownMinMs","BotGrenadeCooldownMaxMs","BotGrenadeTeamGapMs"},
   new[]{"0","0","0","0","15000","20000","3000"},new[]{"0","1","0","0","12000","18000","2500"},new[]{"0","2","1","1","8000","12000","1500"},new[]{"1","3","1","1","3000","6000","500"}),
  new Tuning("Lançadores de foguete","Bots usam lança-foguetes contra veículos.",YN,new[]{"BotLauncherSupport"},new[]{"0"},new[]{"1"}),
  new Tuning("Veículos","Bots entram em tanques, blindados e jatos.",YN,new[]{"VehicleBots"},new[]{"0"},new[]{"1"}),
  new Tuning("Nascer na linha de frente","Bots renascem perto do combate em vez de só na base.",YN,new[]{"BotFrontlineSpawn"},new[]{"0"},new[]{"1"}),
  new Tuning("Nascer junto do esquadrão","Bots renascem no colega de esquadrão e andam mais agrupados.",YN,new[]{"BotSquadSpawn"},new[]{"0"},new[]{"1"}),
  new Tuning("Reanimação","Médicos reanimam caídos. Intensidade = quanto tempo o caído espera (3 s, 6 s, 10 s).",L4,new[]{"BotMedicRevive","BotAcceptRevive","BotReviveWindowMs"},new[]{"0","0","6000"},new[]{"1","1","3000"},new[]{"1","1","6000"},new[]{"1","1","10000"}),
  new Tuning("Foco nas bandeiras","Quanto do time vai capturar objetivos (40%, 65%, 95%). Desligado = não capturam.",L4,new[]{"CaptureObjectives","ObjectiveShare"},new[]{"0","0.20"},new[]{"1","0.40"},new[]{"1","0.65"},new[]{"1","0.95"}),
  new Tuning("Avanço no território inimigo","Quanto os bots livres avançam para o lado do inimigo.",L3,new[]{"RoamerDepthWeight"},new[]{"0.35"},new[]{"0.60"},new[]{"0.85"}),
  new Tuning("Agilidade de movimento","Velocidade para virar e para se soltar quando ficam presos.",L3,new[]{"SteeringResponseSeconds","StuckSeconds"},new[]{"0.4","2.0"},new[]{"0.3","1.5"},new[]{"0.2","1.0"}),
  new Tuning("Chat dos bots","Mensagens dos bots no chat. Alta = conversa casual e resumo no fim da rodada.",L4,new[]{"BotMatchChat","BotCasualChat","BotRoundChat","BotMatchChatMinIntervalMs","BotMatchChatMaxIntervalMs"},
   new[]{"0","0","0","5000","8000"},new[]{"1","0","0","20000","30000"},new[]{"1","1","0","10000","15000"},new[]{"1","1","1","5000","8000"}),
 };
 // Perfis rápidos: um nível por ajuste, na ordem de All.
 public static readonly string[] PresetNames={"Leve","Equilibrado","Intenso","Original do mod"};
 public static readonly int[][] Presets={
  new[]{1,1,2,1,1,0,0,0,1,1,2,0,1,0},
  new[]{1,1,3,2,2,1,1,0,1,2,2,1,2,1},
  new[]{1,1,3,3,3,1,1,1,1,3,3,2,2,2},
  new[]{1,1,3,3,3,1,1,1,1,2,3,2,2,0},
 };
 static string Ini(string root){return Path.Combine(root,"Server\\dinput8_bf4hook.ini");}
 public static string Text(string root){return Config.Decode(File.ReadAllBytes(Ini(root)));}
 // Nível atual de cada ajuste (-1 = valores que não batem com nenhum nível: personalizado).
 public static int[] Detect(string text){
  return All.Select(t=>{for(int i=0;i<t.Values.Length;i++)if(t.Values[i].All(kv=>Same(Config.Read(text,kv.Key),kv.Value)))return i;return -1;}).ToArray();
 }
 static bool Same(string a,string b){if(a==null)return false;double x,y;var c=System.Globalization.CultureInfo.InvariantCulture;if(Double.TryParse(a,System.Globalization.NumberStyles.Float,c,out x)&&Double.TryParse(b,System.Globalization.NumberStyles.Float,c,out y))return Math.Abs(x-y)<1e-9;return a==b;}
 // Grava com backup (mesma rotina dos perfis). Ajustes sem escolha (-1) ficam como estão.
 public static string Save(string root,int[] choice){
  var p=new Profile("Ajustes finos","","",Profile.All[2].Values.Values.ToArray());p.Values.Clear();
  for(int i=0;i<All.Length;i++)if(choice[i]>=0)foreach(var kv in All[i].Values[choice[i]])p.Values[kv.Key]=kv.Value;
  return Config.Save(root,p,false);
 }
}
// Janela "Ajustes dos bots".
public sealed class BotTuningForm : Form {
 readonly string root;readonly int[] choice;readonly List<RadioButton[]> rows=new List<RadioButton[]>();readonly Label status;readonly Func<string,Action<string>,Task> afterSave;bool busy;
 public BotTuningForm(string root,Func<string,Action<string>,Task> afterSave){
  this.root=root;this.afterSave=afterSave;
  Text="BF4 • Ajustes dos bots";StartPosition=FormStartPosition.CenterParent;ClientSize=new Size(940,760);MinimumSize=new Size(820,600);BackColor=Theme.Background;ForeColor=Theme.Ink;Font=Theme.Body;AutoScaleMode=AutoScaleMode.Dpi;MinimizeBox=false;ShowInTaskbar=false;
  try{Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);}catch{}
  choice=BotTuning.Detect(BotTuning.Text(root));
  var shell=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(22,16,22,14),ColumnCount=1,RowCount=5};
  foreach(int h in new[]{44,40,52,0,58})shell.RowStyles.Add(h==0?new RowStyle(SizeType.Percent,100):new RowStyle(SizeType.Absolute,h));
  shell.Controls.Add(new Label{Text="Ajustes dos bots",Font=Theme.Heading,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,0);
  shell.Controls.Add(new Label{Text="Escolha a intensidade de cada comportamento. Vale quando o servidor reinicia; com o PC servidor conectado, a Central envia e reinicia lá sozinha. A mira e a cadência de tiro continuam as do mod.",ForeColor=Theme.Muted,Dock=DockStyle.Fill},0,1);
  var presets=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,Padding=new Padding(0,6,0,0)};
  presets.Controls.Add(new Label{Text="Perfis rápidos:",AutoSize=false,Width=120,Height=36,TextAlign=ContentAlignment.MiddleLeft,Font=new Font(Theme.Body,FontStyle.Bold)});
  for(int k=0;k<BotTuning.PresetNames.Length;k++){int idx=k;var b=new ActionButton{Text=BotTuning.PresetNames[k],Width=150,Height=36,BackColor=Theme.Surface,ForeColor=Theme.Ink,Margin=new Padding(4,0,4,0)};b.Click+=(s,e)=>Apply(BotTuning.Presets[idx]);presets.Controls.Add(b);}
  shell.Controls.Add(presets,0,2);
  var list=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,AutoScroll=true,BackColor=Theme.Surface,Padding=new Padding(16,8,16,8)};
  list.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));list.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,440));
  for(int i=0;i<BotTuning.All.Length;i++){
   var t=BotTuning.All[i];int row=i;list.RowStyles.Add(new RowStyle(SizeType.Absolute,66));
   var text=new Label{Dock=DockStyle.Fill,Text=t.Title+(choice[i]<0?"   (atual: personalizado)":""),Font=new Font(Theme.Body,FontStyle.Bold),TextAlign=ContentAlignment.BottomLeft,Margin=new Padding(0,6,8,0)};
   var hint=new Label{Dock=DockStyle.Fill,Text=t.Hint,ForeColor=Theme.Muted,Font=new Font("Segoe UI",9),Margin=new Padding(0,0,8,6)};
   var left=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=2,ColumnCount=1,Margin=Padding.Empty};left.RowStyles.Add(new RowStyle(SizeType.Percent,45));left.RowStyles.Add(new RowStyle(SizeType.Percent,55));left.Controls.Add(text,0,0);left.Controls.Add(hint,0,1);
   var options=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,Padding=new Padding(0,14,0,0),Margin=Padding.Empty};
   var buttons=new RadioButton[t.Levels.Length];
   for(int l=0;l<t.Levels.Length;l++){int level=l;var r=new RadioButton{Text=t.Levels[l],Appearance=Appearance.Button,FlatStyle=FlatStyle.Flat,TextAlign=ContentAlignment.MiddleCenter,Width=t.Levels.Length==2?150:104,Height=34,Margin=new Padding(0,0,4,0),Cursor=Cursors.Hand,AccessibleName=t.Title+": "+t.Levels[l]};
    r.FlatAppearance.BorderColor=Theme.Border;r.FlatAppearance.CheckedBackColor=Theme.Ink;r.Checked=choice[i]==l;PaintChoice(r);
    r.CheckedChanged+=(s,e)=>{PaintChoice(r);if(r.Checked){choice[row]=level;text.Text=t.Title;}};
    buttons[l]=r;options.Controls.Add(r);}
   rows.Add(buttons);
   list.Controls.Add(left,0,i);list.Controls.Add(options,1,i);
  }
  shell.Controls.Add(list,0,3);
  var bottom=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,Padding=new Padding(0,10,0,0)};bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,190));bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,130));
  status=new Label{Dock=DockStyle.Fill,ForeColor=Theme.Muted,TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true};bottom.Controls.Add(status,0,0);
  var save=new ActionButton{Text="Salvar e aplicar",Dock=DockStyle.Fill,BackColor=Theme.Ink,ForeColor=Color.White,Margin=new Padding(4,0,4,0)};save.Click+=async(s,e)=>await Save(save);bottom.Controls.Add(save,1,0);
  var cancel=new ActionButton{Text="Fechar",Dock=DockStyle.Fill,BackColor=Theme.Surface,ForeColor=Theme.Ink,Margin=new Padding(4,0,0,0)};cancel.Click+=(s,e)=>Close();bottom.Controls.Add(cancel,2,0);
  shell.Controls.Add(bottom,0,4);Controls.Add(shell);
  FormClosing+=(s,e)=>{if(busy){e.Cancel=true;status.Text="Aguarde terminar de aplicar.";}};
 }
 static void PaintChoice(RadioButton r){r.BackColor=r.Checked?Theme.Ink:Theme.Surface;r.ForeColor=r.Checked?Color.White:Theme.Ink;r.Font=new Font(Theme.Body,r.Checked?FontStyle.Bold:FontStyle.Regular);}
 void Apply(int[] preset){for(int i=0;i<rows.Count;i++)rows[i][preset[i]].Checked=true;status.Text="Perfil rápido escolhido. Clique em Salvar e aplicar.";}
 void Say(string text){if(IsDisposed)return;if(InvokeRequired){BeginInvoke(new Action(()=>Say(text)));return;}status.Text=text;}
 async Task Save(ActionButton button){
  if(busy)return;busy=true;button.State=ActionState.Busy;
  try{
   string backup=await Task.Run(()=>BotTuning.Save(root,(int[])choice.Clone()));
   Say("Salvo (backup guardado).");
   if(afterSave!=null)await afterSave(BotTuning.Text(root),Say);
   button.Flash(true);
  }catch(Exception ex){Say(ex.Message);button.Flash(false);}
  finally{busy=false;}
 }
}
}
