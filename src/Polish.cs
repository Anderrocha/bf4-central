using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
namespace BF4Bots {

public enum ActionState { Idle, Busy, Success, Error }

// Botão com estados visuais: repouso, foco, mouse por cima, clique, em andamento, concluído e falha.
public sealed class ActionButton : Button {
 bool hover,pressed;ActionState state=ActionState.Idle;readonly Timer spin=new Timer{Interval=40};readonly Timer revert=new Timer();float angle;
 public ActionButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;spin.Tick+=(s,e)=>{angle=(angle+18)%360;Invalidate();};revert.Tick+=(s,e)=>{revert.Stop();State=ActionState.Idle;};L.Changed+=Fit;}
 // Em painéis de fluxo, alarga o botão quando a tradução não cabe (até 1,6× a largura original).
 int baseWidth=-1;
 void Fit(){if(IsDisposed||!IsHandleCreated||Dock!=DockStyle.None||!(Parent is FlowLayoutPanel))return;if(baseWidth<0)baseWidth=Width;if(Parent.Width<baseWidth*1.6)return;int need=TextRenderer.MeasureText(L.T(Text),Font).Width+36;Width=Math.Min((int)(baseWidth*1.6),Math.Max(baseWidth,need));}
 protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);Fit();}
 protected override void OnTextChanged(EventArgs e){base.OnTextChanged(e);if(baseWidth>=0)Fit();}
 public ActionState State{get{return state;}set{state=value;revert.Stop();if(value==ActionState.Busy)spin.Start();else spin.Stop();AccessibleDescription=value==ActionState.Busy?"Em andamento":value==ActionState.Success?"Concluído":value==ActionState.Error?"Falhou":null;Invalidate();}}
 public void Flash(bool ok){State=ok?ActionState.Success:ActionState.Error;revert.Interval=ok?1600:2400;revert.Start();}
 protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
 protected override void OnMouseLeave(EventArgs e){hover=false;pressed=false;Invalidate();base.OnMouseLeave(e);}
 protected override void OnMouseDown(MouseEventArgs e){if(e.Button==MouseButtons.Left){pressed=true;Invalidate();}base.OnMouseDown(e);}
 protected override void OnMouseUp(MouseEventArgs e){pressed=false;Invalidate();base.OnMouseUp(e);}
 protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Space){pressed=true;Invalidate();}base.OnKeyDown(e);}
 protected override void OnKeyUp(KeyEventArgs e){pressed=false;Invalidate();base.OnKeyUp(e);}
 protected override void OnEnabledChanged(EventArgs e){Cursor=Enabled?Cursors.Hand:Cursors.Default;Invalidate();base.OnEnabledChanged(e);}
 static bool Dark(Color c){return c.R*0.299+c.G*0.587+c.B*0.114<140;}
 public static Color Mix(Color a,Color b,float t){return Color.FromArgb((int)(a.R+(b.R-a.R)*t),(int)(a.G+(b.G-a.G)*t),(int)(a.B+(b.B-a.B)*t));}
 public static GraphicsPath Round(RectangleF r,float radius){var p=new GraphicsPath();float d=radius*2;if(d<=0){p.AddRectangle(r);return p;}p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
 protected override void OnPaint(PaintEventArgs e){
  var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Parent!=null?Parent.BackColor:Theme.Background);
  Color baseColor=BackColor,fill,border,text;bool dark=Dark(baseColor);
  if(state==ActionState.Busy){fill=Theme.Accent;border=Theme.Accent;text=Color.White;}
  else if(state==ActionState.Success){fill=Theme.Success;border=Theme.Success;text=Color.White;}
  else if(state==ActionState.Error){fill=Theme.Danger;border=Theme.Danger;text=Color.White;}
  else if(!Enabled){fill=dark?Mix(baseColor,Theme.Background,0.55f):Mix(baseColor,Theme.Background,0.6f);border=Theme.Border;text=dark?Mix(Color.White,baseColor,0.45f):Theme.Muted;}
  else if(pressed){fill=dark?Theme.Accent:Theme.Pressed;border=Theme.Accent;text=dark?Color.White:Theme.Ink;}
  else if(hover){fill=dark?Mix(baseColor,Color.White,0.14f):Theme.Hover;border=dark?fill:Theme.Ink;text=dark?Color.White:Theme.Ink;}
  else{fill=baseColor;border=dark?baseColor:Theme.Border;text=ForeColor;}
  var r=new RectangleF(0.5f,0.5f,Width-1.5f,Height-1.5f);
  if(pressed&&Enabled&&state==ActionState.Idle)r.Offset(0,1);
  using(var path=Round(r,6))using(var brush=new SolidBrush(fill))using(var pen=new Pen(border,1f)){g.FillPath(brush,path);g.DrawPath(pen,path);}
  if(Focused&&ShowFocusCues&&state==ActionState.Idle)using(var path=Round(RectangleF.Inflate(r,-3,-3),4))using(var pen=new Pen(dark?Color.FromArgb(180,255,255,255):Theme.Accent,1.6f)){pen.DashStyle=DashStyle.Solid;g.DrawPath(pen,path);}
  string label=L.T(Text);int iconWidth=state==ActionState.Idle?0:20;var size=TextRenderer.MeasureText(g,label,Font,Size.Empty,TextFormatFlags.SingleLine);int total=Math.Min(Width-16,size.Width+iconWidth);int x=(Width-total)/2;
  if(iconWidth>0){var icon=new RectangleF(x,(Height-14)/2f,14,14);using(var pen=new Pen(text,2f){StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round}){if(state==ActionState.Busy){using(var faint=new Pen(Color.FromArgb(70,text),2f))g.DrawEllipse(faint,icon);g.DrawArc(pen,icon,angle,100);}else if(state==ActionState.Success)g.DrawLines(pen,new[]{new PointF(icon.X+1.5f,icon.Y+7.5f),new PointF(icon.X+5.5f,icon.Y+11.5f),new PointF(icon.X+12.5f,icon.Y+3f)});else{g.DrawLine(pen,icon.X+2,icon.Y+2,icon.Right-2,icon.Bottom-2);g.DrawLine(pen,icon.Right-2,icon.Y+2,icon.X+2,icon.Bottom-2);}}}
  var bounds=new Rectangle(x+iconWidth,0,Math.Max(1,total-iconWidth),Height);if(pressed&&Enabled&&state==ActionState.Idle)bounds.Offset(0,1);
  TextRenderer.DrawText(g,label,Font,bounds,text,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis);
 }
 protected override void Dispose(bool disposing){if(disposing){L.Changed-=Fit;spin.Dispose();revert.Dispose();}base.Dispose(disposing);}
}

public enum StepState { Pending, Active, Done, Error, Skipped }
public enum StatusKind { Info, Busy, Success, Error }
public sealed class Step {
 public string Label;public string[] Triggers;public StepState State;
 public Step(string label,params string[] triggers){Label=label;Triggers=triggers;}
}

// Modelo único da etapa atual, compartilhado pela barra inferior e pela tela final.
public sealed class StepTracker {
 public event Action Changed;
 public string Message="";public StatusKind Kind=StatusKind.Info;public List<Step> Steps=new List<Step>();
 public bool HasSteps{get{return Steps.Count>0;}}
 void Raise(){var h=Changed;if(h!=null)h();}
 public void Begin(IEnumerable<Step> steps){Steps=steps==null?new List<Step>():steps.ToList();foreach(var s in Steps)s.State=StepState.Pending;if(Steps.Count>0)Steps[0].State=StepState.Active;Raise();}
 public void Clear(){Steps=new List<Step>();Raise();}
 public void Skip(string label){var s=Steps.FirstOrDefault(x=>x.Label==label);if(s!=null&&s.State==StepState.Pending){s.State=StepState.Skipped;Raise();}}
 public void Report(string message,StatusKind kind){Message=message??"";Kind=kind;if(kind==StatusKind.Error){var active=Steps.FirstOrDefault(x=>x.State==StepState.Active)??Steps.FirstOrDefault(x=>x.State==StepState.Pending);if(active!=null)active.State=StepState.Error;}else if(kind==StatusKind.Busy){int index=Steps.FindIndex(x=>x.Triggers.Any(t=>Message.StartsWith(t,StringComparison.OrdinalIgnoreCase)));if(index>=0)Advance(index);}Raise();}
 void Advance(int index){for(int i=0;i<Steps.Count;i++){var s=Steps[i];if(s.State==StepState.Skipped||s.State==StepState.Error)continue;if(i<index)s.State=StepState.Done;else if(i==index)s.State=StepState.Active;}}
 public void Complete(){foreach(var s in Steps)if(s.State!=StepState.Skipped&&s.State!=StepState.Error)s.State=StepState.Done;Kind=Steps.Any(x=>x.State==StepState.Error)?StatusKind.Error:StatusKind.Success;Raise();}
 public void Finish(bool ok){if(ok)Complete();else{Kind=StatusKind.Error;var active=Steps.FirstOrDefault(x=>x.State==StepState.Active);if(active!=null)active.State=StepState.Error;Raise();}}
}

// Barra de estado: ícone do resultado, mensagem e, quando há uma sequência, cada etapa com ✓ ao ser confirmada.
public sealed class StatusBar : Control {
 readonly StepTracker model;readonly Timer spin=new Timer{Interval=40};float angle;public bool Large;public bool Centered;
 public StatusBar(StepTracker tracker){model=tracker;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw|ControlStyles.SupportsTransparentBackColor,true);BackColor=Theme.Surface;ForeColor=Theme.Ink;Font=Theme.Body;AccessibleRole=AccessibleRole.StatusBar;spin.Tick+=(s,e)=>{angle=(angle+16)%360;Invalidate();};tracker.Changed+=OnModel;}
 public int PreferredHeight{get{return model.HasSteps?(Large?44:78):50;}}
 public event Action HeightChanged;bool hadSteps;
 void OnModel(){if(IsDisposed)return;bool animate=model.Kind==StatusKind.Busy||model.Steps.Any(s=>s.State==StepState.Active&&model.Kind==StatusKind.Busy);if(animate)spin.Start();else spin.Stop();Text=model.Message;AccessibleName=L.T(model.Kind==StatusKind.Success?"Concluído: ":model.Kind==StatusKind.Error?"Falha: ":"")+L.T(model.Message)+(model.HasSteps?". "+String.Join(", ",model.Steps.Select(s=>L.T(s.Label)+" "+L.T(Describe(s.State)))):"");AccessibilityNotifyClients(AccessibleEvents.NameChange,-1);if(hadSteps!=model.HasSteps){hadSteps=model.HasSteps;var h=HeightChanged;if(h!=null)h();}Invalidate();}
 static string Describe(StepState s){return s==StepState.Done?"concluída":s==StepState.Active?"em andamento":s==StepState.Error?"com falha":s==StepState.Skipped?"não necessária":"pendente";}
 static Color KindColor(StatusKind k){return k==StatusKind.Success?Theme.Success:k==StatusKind.Error?Theme.Danger:k==StatusKind.Busy?Theme.Accent:Theme.Muted;}
 void DrawBadge(Graphics g,RectangleF r,StepState s){
  using(var pen=new Pen(Color.White,Math.Max(1.8f,r.Width/9f)){StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round}){
   if(s==StepState.Done){using(var b=new SolidBrush(Theme.Success))g.FillEllipse(b,r);g.DrawLines(pen,new[]{new PointF(r.X+r.Width*0.26f,r.Y+r.Height*0.52f),new PointF(r.X+r.Width*0.43f,r.Y+r.Height*0.69f),new PointF(r.X+r.Width*0.75f,r.Y+r.Height*0.33f)});}
   else if(s==StepState.Error){using(var b=new SolidBrush(Theme.Danger))g.FillEllipse(b,r);float m=r.Width*0.32f;g.DrawLine(pen,r.X+m,r.Y+m,r.Right-m,r.Bottom-m);g.DrawLine(pen,r.Right-m,r.Y+m,r.X+m,r.Bottom-m);}
   else if(s==StepState.Active){using(var faint=new Pen(Color.FromArgb(60,Theme.Accent),2.2f))g.DrawEllipse(faint,r);using(var arc=new Pen(Theme.Accent,2.2f){StartCap=LineCap.Round,EndCap=LineCap.Round})g.DrawArc(arc,r,model.Kind==StatusKind.Busy?angle:270,model.Kind==StatusKind.Busy?110:360);using(var b=new SolidBrush(Theme.Accent))g.FillEllipse(b,RectangleF.Inflate(r,-r.Width*0.32f,-r.Height*0.32f));}
   else if(s==StepState.Skipped){using(var p=new Pen(Theme.Border,1.6f){DashStyle=DashStyle.Dot})g.DrawEllipse(p,r);using(var p=new Pen(Theme.Muted,1.6f))g.DrawLine(p,r.X+r.Width*0.32f,r.Y+r.Height/2,r.Right-r.Width*0.32f,r.Y+r.Height/2);}
   else using(var p=new Pen(Theme.Border,1.8f))g.DrawEllipse(p,r);
  }
 }
 void DrawKind(Graphics g,RectangleF r){
  if(model.Kind==StatusKind.Busy){using(var faint=new Pen(Color.FromArgb(60,Theme.Accent),2f))g.DrawEllipse(faint,r);using(var arc=new Pen(Theme.Accent,2f){StartCap=LineCap.Round,EndCap=LineCap.Round})g.DrawArc(arc,r,angle,110);}
  else if(model.Kind==StatusKind.Success)DrawBadge(g,r,StepState.Done);
  else if(model.Kind==StatusKind.Error)DrawBadge(g,r,StepState.Error);
  else{using(var b=new SolidBrush(Theme.Muted))g.FillEllipse(b,r);using(var w=new SolidBrush(Color.White)){g.FillRectangle(w,r.X+r.Width/2-1,r.Y+r.Height*0.42f,2,r.Height*0.36f);g.FillEllipse(w,r.X+r.Width/2-1.3f,r.Y+r.Height*0.2f,2.6f,2.6f);}}
 }
 protected override void OnPaint(PaintEventArgs e){
  var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(BackColor);
  if(!Large)using(var accent=new SolidBrush(KindColor(model.Kind)))g.FillRectangle(accent,0,0,4,Height);
  int left=Large?0:18;float badge=Large?22:16;string message=L.T(model.Message);
  if(Large&&!model.HasSteps)return;
  if(model.HasSteps){
   int count=model.Steps.Count;float top=10;float slot=(Width-left*2f)/count;
   using(var stepFont=new Font(Font.FontFamily,Large?10f:9f,FontStyle.Bold)){
    for(int i=0;i<count;i++){var s=model.Steps[i];string stepLabel=L.T(s.Label);float cx=left+slot*i;var r=new RectangleF(cx,top,badge,badge);
     if(i<count-1){float x1=cx+badge+6+Math.Min(slot-badge-12,TextRenderer.MeasureText(stepLabel,stepFont).Width)+8,x2=left+slot*(i+1)-6;if(x2>x1)using(var line=new Pen(s.State==StepState.Done?Theme.Success:Theme.Border,2f))g.DrawLine(line,x1,top+badge/2,x2,top+badge/2);}
     DrawBadge(g,r,s.State);Color ink=s.State==StepState.Pending||s.State==StepState.Skipped?Theme.Muted:s.State==StepState.Error?Theme.Danger:s.State==StepState.Done?Theme.Success:Theme.Ink;
     TextRenderer.DrawText(g,stepLabel,stepFont,new Rectangle((int)(cx+badge+6),(int)top-2,(int)Math.Max(1,slot-badge-12),(int)badge+4),ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis);}
   }
   if(Large)return;
   float msgTop=top+badge+12;var msg=new Rectangle(left,(int)msgTop,Math.Max(1,Width-left*2),(int)Math.Max(1,Height-msgTop-4));
   TextRenderer.DrawText(g,message,Font,msg,model.Kind==StatusKind.Error?Theme.Danger:Large?Theme.Muted:Theme.Ink,(Centered?TextFormatFlags.HorizontalCenter:TextFormatFlags.Left)|TextFormatFlags.Top|TextFormatFlags.WordBreak|TextFormatFlags.EndEllipsis);
  }else{
   float iconSize=16;var size=TextRenderer.MeasureText(g,message,Font,new Size(Math.Max(1,Width-left*2-26),Height),TextFormatFlags.WordBreak);int start=Centered?Math.Max(left,(Width-size.Width-26)/2):left;
   var icon=new RectangleF(start,(Height-iconSize)/2f,iconSize,iconSize);DrawKind(g,icon);
   TextRenderer.DrawText(g,message,Font,new Rectangle(start+26,0,Math.Max(1,Width-start-26-left),Height),model.Kind==StatusKind.Error?Theme.Danger:Theme.Ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.WordBreak|TextFormatFlags.EndEllipsis);
  }
 }
 protected override void Dispose(bool disposing){if(disposing){spin.Dispose();model.Changed-=OnModel;}base.Dispose(disposing);}
}
}
