using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
// Peças visuais do painel, na mesma paleta e tipografia da BF4 Central (DESIGN.md).
namespace BF4Servidor {
static class Theme {
 public static readonly Color Background=ColorTranslator.FromHtml("#E8EDF0"),Surface=Color.White,Ink=ColorTranslator.FromHtml("#1E3442"),Muted=ColorTranslator.FromHtml("#4D626E"),Accent=ColorTranslator.FromHtml("#A34D14"),Border=ColorTranslator.FromHtml("#C4D0D7"),Success=ColorTranslator.FromHtml("#1E7D46"),Danger=ColorTranslator.FromHtml("#B3261E"),Warning=ColorTranslator.FromHtml("#B26A00"),Hover=ColorTranslator.FromHtml("#F2F6F8"),Pressed=ColorTranslator.FromHtml("#F6E3D5");
 public static readonly Font Body=new Font("Segoe UI",10),Bold=new Font("Segoe UI",10,FontStyle.Bold),Small=new Font("Segoe UI",9),Title=new Font("Bahnschrift",22,FontStyle.Bold),Heading=new Font("Bahnschrift",16,FontStyle.Bold),Big=new Font("Bahnschrift",20,FontStyle.Bold);
 public static GraphicsPath Round(RectangleF r,float radius){var p=new GraphicsPath();float d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
 public static Color Mix(Color a,Color b,float t){return Color.FromArgb((int)(a.R+(b.R-a.R)*t),(int)(a.G+(b.G-a.G)*t),(int)(a.B+(b.B-a.B)*t));}
}
// Botão arredondado com estados (mouse por cima, clique, desativado, em andamento).
sealed class PillButton : Button {
 bool hover,pressed,busy;float angle;readonly Timer spin=new Timer{Interval=40};
 public PillButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;BackColor=Theme.Surface;ForeColor=Theme.Ink;Font=Theme.Bold;spin.Tick+=(s,e)=>{angle=(angle+18)%360;Invalidate();};}
 public bool Busy{get{return busy;}set{busy=value;if(value)spin.Start();else spin.Stop();Invalidate();}}
 protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
 protected override void OnMouseLeave(EventArgs e){hover=false;pressed=false;Invalidate();base.OnMouseLeave(e);}
 protected override void OnMouseDown(MouseEventArgs e){pressed=e.Button==MouseButtons.Left;Invalidate();base.OnMouseDown(e);}
 protected override void OnMouseUp(MouseEventArgs e){pressed=false;Invalidate();base.OnMouseUp(e);}
 protected override void OnEnabledChanged(EventArgs e){Cursor=Enabled?Cursors.Hand:Cursors.Default;Invalidate();base.OnEnabledChanged(e);}
 static bool Dark(Color c){return c.R*0.299+c.G*0.587+c.B*0.114<140;}
 protected override void OnPaint(PaintEventArgs e){
  var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Parent!=null?Parent.BackColor:Theme.Background);
  Color fill=BackColor,border,text=ForeColor;bool dark=Dark(BackColor);
  if(busy){fill=Theme.Accent;text=Color.White;}
  else if(!Enabled){fill=Theme.Mix(BackColor,Theme.Background,0.6f);text=dark?Theme.Mix(Color.White,BackColor,0.45f):Theme.Muted;}
  else if(pressed){fill=dark?Theme.Accent:Theme.Pressed;}
  else if(hover){fill=dark?Theme.Mix(BackColor,Color.White,0.14f):Theme.Hover;}
  border=dark||busy?fill:(hover?Theme.Ink:Theme.Border);
  var r=new RectangleF(0.5f,0.5f,Width-1.5f,Height-1.5f);
  using(var path=Theme.Round(r,7))using(var brush=new SolidBrush(fill))using(var pen=new Pen(border)){g.FillPath(brush,path);g.DrawPath(pen,path);}
  if(Focused&&ShowFocusCues)using(var path=Theme.Round(RectangleF.Inflate(r,-3,-3),5))using(var pen=new Pen(dark?Color.FromArgb(180,255,255,255):Theme.Accent,1.6f))g.DrawPath(pen,path);
  int icon=busy?22:0;var size=TextRenderer.MeasureText(g,Text,Font);int total=Math.Min(Width-16,size.Width+icon);int x=(Width-total)/2;
  if(busy)using(var faint=new Pen(Color.FromArgb(70,text),2f))using(var pen=new Pen(text,2f)){var box=new RectangleF(x,(Height-14)/2f,14,14);g.DrawEllipse(faint,box);g.DrawArc(pen,box,angle,100);}
  TextRenderer.DrawText(g,Text,Font,new Rectangle(x+icon,0,Math.Max(1,total-icon),Height),text,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis);
 }
 protected override void Dispose(bool disposing){if(disposing)spin.Dispose();base.Dispose(disposing);}
}
// Interruptor em pílula com texto explícito "Ativado/Desativado" (igual ao da Central).
sealed class Switch : CheckBox {
 public string Caption,Hint;
 public Switch(){SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);Cursor=Cursors.Hand;Height=50;}
 protected override void OnPaint(PaintEventArgs e){
  var g=e.Graphics;g.Clear(BackColor);g.SmoothingMode=SmoothingMode.AntiAlias;int y=Hint==null?(Height-22)/2:6;
  using(var brush=new SolidBrush(Checked?Theme.Ink:Theme.Border)){g.FillEllipse(brush,2,y,22,22);g.FillRectangle(brush,13,y,21,22);g.FillEllipse(brush,23,y,22,22);}
  g.FillEllipse(Brushes.White,Checked?25:4,y+2,18,18);
  TextRenderer.DrawText(g,Caption+"  •  "+(Checked?"Ativado":"Desativado"),Theme.Bold,new Rectangle(56,y,Math.Max(1,Width-58),22),Theme.Ink,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
  if(Hint!=null)TextRenderer.DrawText(g,Hint,Theme.Small,new Rectangle(56,y+22,Math.Max(1,Width-58),Height-y-22),Theme.Muted,TextFormatFlags.WordBreak|TextFormatFlags.EndEllipsis);
  if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,ClientRectangle);
 }
 protected override void OnCheckedChanged(EventArgs e){base.OnCheckedChanged(e);Invalidate();}
}
// Cartão branco com cantos arredondados sobre o fundo azul-acinzentado.
sealed class Card : Panel {
 public Card(){DoubleBuffered=true;BackColor=Theme.Surface;Padding=new Padding(18,14,18,14);Margin=new Padding(0,0,0,12);}
 protected override void OnPaintBackground(PaintEventArgs e){var g=e.Graphics;g.Clear(Parent!=null?Parent.BackColor:Theme.Background);g.SmoothingMode=SmoothingMode.AntiAlias;using(var path=Theme.Round(new RectangleF(0.5f,0.5f,Width-1.5f,Height-1.5f),9))using(var b=new SolidBrush(Theme.Surface))using(var pen=new Pen(Theme.Border)){g.FillPath(b,path);g.DrawPath(pen,path);}}
}
// Bolinha de estado (verde/âmbar/cinza) — o texto ao lado sempre diz o estado.
sealed class Dot : Control {
 public Color Color=Theme.Muted;public bool Hollow;
 public Dot(){SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.SupportsTransparentBackColor,true);Size=new Size(18,18);}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(Parent!=null?Parent.BackColor:Theme.Surface);g.SmoothingMode=SmoothingMode.AntiAlias;var r=new Rectangle(2,2,Width-5,Height-5);if(Hollow)using(var p=new Pen(Color,2))g.DrawEllipse(p,r);else{using(var halo=new SolidBrush(Color.FromArgb(50,Color)))g.FillEllipse(halo,0,0,Width-1,Height-1);using(var b=new SolidBrush(Color))g.FillEllipse(b,Rectangle.Inflate(r,-2,-2));}}
}
// Cabeçalho com a arte do BF4 (quando embutida) e degradê escuro para o título branco.
sealed class Header : Panel {
 readonly Image art;public string Title,Subtitle;
 public Header(Image art){this.art=art;DoubleBuffered=true;BackColor=Theme.Ink;}
 protected override void OnPaint(PaintEventArgs e){
  var g=e.Graphics;g.Clear(Theme.Ink);
  if(art!=null){float s=Math.Max((float)Width/art.Width,(float)Height/art.Height);float w=Width/s,h=Height/s;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(art,ClientRectangle,new RectangleF((art.Width-w)/2,Math.Min(art.Height-h,art.Height*0.08f),w,h),GraphicsUnit.Pixel);}
  using(var shade=new LinearGradientBrush(ClientRectangle,Color.FromArgb(230,12,29,40),Color.FromArgb(art==null?255:80,12,29,40),0f))g.FillRectangle(shade,ClientRectangle);
  using(var accent=new SolidBrush(Theme.Accent))g.FillRectangle(accent,0,Height-4,Width,4);
  TextRenderer.DrawText(g,Title,Theme.Title,new Point(24,Height/2-38),Color.White);
  TextRenderer.DrawText(g,Subtitle,Theme.Body,new Rectangle(26,Height/2+6,Width-40,40),Color.FromArgb(215,229,237),TextFormatFlags.WordBreak);
 }
 protected override void Dispose(bool disposing){if(disposing&&art!=null)art.Dispose();base.Dispose(disposing);}
}
// Faixa de mensagens com a barra lateral colorida, como a da Central.
sealed class StatusStrip2 : Control {
 public Color Mark=Theme.Ink;
 public StatusStrip2(){SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);Height=46;}
 protected override void OnTextChanged(EventArgs e){base.OnTextChanged(e);AccessibleName=Text;Invalidate();AccessibilityNotifyClients(AccessibleEvents.NameChange,-1);}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(Theme.Surface);using(var b=new SolidBrush(Mark))g.FillRectangle(b,0,0,4,Height);TextRenderer.DrawText(g,Text,Theme.Body,new Rectangle(16,0,Width-24,Height),Theme.Ink,TextFormatFlags.VerticalCenter|TextFormatFlags.WordBreak|TextFormatFlags.EndEllipsis);}
}
}
