using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace BF4Bots {
public static class PolishTests {
 static Step[] Launch(){return new[]{new Step("Verificação","Preparando o início","Verificando"),new Step("Backend","Preparando backend"),new Step("Servidor","Preparando servidor"),new Step("Launcher","Preparando o launcher"),new Step("Jogo aberto","Abrindo o Battlefield"),new Step("Conectado","Aguardando o jogo")};}
 public static void Run(string folder){
  var t=new StepTracker();t.Begin(Launch());
  t.Report("Preparando o início…",StatusKind.Busy);if(t.Steps[0].State!=StepState.Active)throw new Exception("Primeira etapa não ficou ativa");
  t.Report("Preparando servidor local e removendo cópias duplicadas desta instalação…",StatusKind.Busy);
  if(t.Steps[0].State!=StepState.Done||t.Steps[1].State!=StepState.Done||t.Steps[2].State!=StepState.Active||t.Steps[3].State!=StepState.Pending)throw new Exception("Avanço de etapas incorreto");
  t.Report("Mensagem sem etapa",StatusKind.Busy);if(t.Steps[2].State!=StepState.Active)throw new Exception("Mensagem livre alterou a etapa");
  t.Report("A porta 25210 está ocupada por outro programa.",StatusKind.Error);if(t.Steps[2].State!=StepState.Error||t.Steps[3].State!=StepState.Pending||t.Kind!=StatusKind.Error)throw new Exception("Falha não marcou a etapa ativa");
  t.Finish(false);if(t.Steps.Count(s=>s.State==StepState.Done)!=2)throw new Exception("Falha marcou etapas não confirmadas como concluídas");
  t.Begin(Launch());t.Report("Abrindo o Battlefield 4…",StatusKind.Busy);t.Finish(true);if(t.Steps.Any(s=>s.State!=StepState.Done)||t.Kind!=StatusKind.Success)throw new Exception("Conclusão não validou todas as etapas");
  using(var form=new Form{ClientSize=new Size(1100,560),BackColor=Theme.Background,Font=Theme.Body,StartPosition=FormStartPosition.Manual,Location=new Point(-3000,-3000),ShowInTaskbar=false}){
   var stack=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(20)};form.Controls.Add(stack);
   Func<StepTracker,StatusBar> bar=m=>{var b=new StatusBar(m){Width=1040,Height=m.HasSteps?78:50,Margin=new Padding(0,0,0,12)};stack.Controls.Add(b);return b;};
   var running=new StepTracker();running.Begin(Launch());running.Report("Preparando o launcher original…",StatusKind.Busy);bar(running);
   var done=new StepTracker();done.Begin(Launch());done.Report("Jogo conectado ao backend offline local. Abra o navegador de servidores no multiplayer.",StatusKind.Busy);done.Finish(true);bar(done);
   var failed=new StepTracker();failed.Begin(Launch());failed.Report("Preparando servidor local…",StatusKind.Busy);failed.Report("O componente não abriu a porta 25210 a tempo. Tente novamente; não será iniciada uma segunda cópia.",StatusKind.Error);failed.Finish(false);bar(failed);
   var map=new StepTracker();map.Begin(new[]{new Step("Serviços locais","Preparando "),new Step("Servidor (RCON)","Conectando"),new Step("Rotação","Gravando"),new Step("Troca de mapa","Carregando "),new Step("Mapa confirmado","Mapa confirmado")});map.Report("Carregando Operation Locker; aguardando confirmação do servidor…",StatusKind.Busy);bar(map);
   var simple=new StepTracker();simple.Report("Conexão salva com backup.",StatusKind.Success);bar(simple);
   var row=new FlowLayoutPanel{Width=1040,Height=52,WrapContents=false};stack.Controls.Add(row);
   foreach(var spec in new[]{Tuple.Create("Primário",true,ActionState.Idle),Tuple.Create("Secundário",false,ActionState.Idle),Tuple.Create("Conectando",true,ActionState.Busy),Tuple.Create("Salvo",false,ActionState.Success),Tuple.Create("Falhou",false,ActionState.Error)}){var b=new ActionButton{Text=spec.Item1,Width=180,Height=38,Margin=new Padding(0,0,12,0),BackColor=spec.Item2?Theme.Ink:Theme.Surface,ForeColor=spec.Item2?Color.White:Theme.Ink};row.Controls.Add(b);b.State=spec.Item3;}
   var disabled=new ActionButton{Text="Desativado",Width=180,Height=38,BackColor=Theme.Ink,ForeColor=Color.White,Enabled=false};row.Controls.Add(disabled);
   form.Show();Application.DoEvents();
   using(var image=new Bitmap(form.ClientSize.Width,form.ClientSize.Height)){form.DrawToBitmap(image,new Rectangle(Point.Empty,form.ClientSize));image.Save(Path.Combine(folder,"polish-states.png"));}
   form.Close();
  }
 }
}
}
