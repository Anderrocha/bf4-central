using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace BF4Bots {
public sealed class PerformanceSwitch : CheckBox {
 public PerformanceSwitch(){SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer,true);Cursor=Cursors.Hand;}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(BackColor);g.SmoothingMode=SmoothingMode.AntiAlias;int y=(Height-22)/2;using(var brush=new SolidBrush(Checked?Theme.Ink:Theme.Border)){g.FillEllipse(brush,2,y,22,22);g.FillRectangle(brush,13,y,21,22);g.FillEllipse(brush,23,y,22,22);}g.FillEllipse(Brushes.White,Checked?25:4,y+2,18,18);TextRenderer.DrawText(g,"Modo leve • "+(Checked?"Ativado":"Desativado"),Font,new Rectangle(54,0,Math.Max(1,Width-56),Height),Enabled?Theme.Ink:Theme.Muted,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);if(Focused)ControlPaint.DrawFocusRectangle(g,ClientRectangle);}
 protected override void OnCheckedChanged(EventArgs e){base.OnCheckedChanged(e);Invalidate();}
}
public sealed class BannerPanel : Panel {
 readonly Image image;
 public BannerPanel(Image image){this.image=image;DoubleBuffered=true;BackColor=Theme.Ink;Dock=DockStyle.Fill;Margin=new Padding(0,0,0,12);}
 protected override void OnPaintBackground(PaintEventArgs e){e.Graphics.Clear(Theme.Ink);if(image==null||Width<1||Height<1)return;float scale=Math.Max((float)Width/image.Width,(float)Height/image.Height);float w=Width/scale,h=Height/scale;var source=new RectangleF((image.Width-w)/2,Math.Min(image.Height-h,image.Height*0.08f),w,h);e.Graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;e.Graphics.DrawImage(image,ClientRectangle,source,GraphicsUnit.Pixel);using(var shade=new LinearGradientBrush(ClientRectangle,Color.FromArgb(225,12,29,40),Color.FromArgb(70,12,29,40),0f))e.Graphics.FillRectangle(shade,ClientRectangle);}
 protected override void Dispose(bool disposing){if(disposing&&image!=null)image.Dispose();base.Dispose(disposing);}
}
public sealed class NavigationTabs : TabControl {
 public NavigationTabs(){Dock=DockStyle.Fill;DrawMode=TabDrawMode.OwnerDrawFixed;SizeMode=TabSizeMode.Fixed;ItemSize=new Size(189,46);Padding=new Point(18,10);SetStyle(ControlStyles.OptimizedDoubleBuffer,true);}
 protected override void OnDrawItem(DrawItemEventArgs e){bool selected=e.Index==SelectedIndex;var bounds=GetTabRect(e.Index);var g=e.Graphics;using(var fill=new SolidBrush(selected?Theme.Ink:Theme.Surface))g.FillRectangle(fill,bounds);Color ink=selected?Color.White:Theme.Ink;if(selected)using(var accent=new SolidBrush(Theme.Accent))g.FillRectangle(accent,bounds.X,bounds.Bottom-4,bounds.Width,4);var icon=new Rectangle(bounds.X+16,bounds.Y+13,21,20);DrawIcon(g,icon,e.Index==0?4:e.Index==1?0:e.Index==2?1:e.Index==3?3:2,ink);string text=(e.Index+1)+". "+(e.Index==2?"Bots":e.Index==3?"Amigos":TabPages[e.Index].Text);using(var font=new Font(Theme.Body.FontFamily,10,FontStyle.Bold))TextRenderer.DrawText(g,text,font,new Rectangle(bounds.X+46,bounds.Y,bounds.Width-50,bounds.Height-3),ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);if(Focused&&selected)ControlPaint.DrawFocusRectangle(g,Rectangle.Inflate(bounds,-6,-6),ink,selected?Theme.Ink:Theme.Surface);}
 static void DrawIcon(Graphics g,Rectangle r,int kind,Color ink){g.SmoothingMode=SmoothingMode.AntiAlias;using(var pen=new Pen(ink,1.7f)){int x=r.X,y=r.Y;if(kind==4){g.DrawRectangle(pen,x+1,y+5,19,14);g.DrawLine(pen,x+6,y+1,x+15,y+1);g.DrawLine(pen,x+10,y+7,x+10,y+15);g.DrawLine(pen,x+7,y+12,x+10,y+15);g.DrawLine(pen,x+13,y+12,x+10,y+15);}else if(kind==0){g.DrawRectangle(pen,x,y+4,21,13);g.DrawLine(pen,x+4,y+10,x+10,y+10);g.DrawLine(pen,x+7,y+7,x+7,y+13);g.DrawEllipse(pen,x+15,y+8,2,2);}else if(kind==1){g.DrawRectangle(pen,x+2,y+4,17,13);g.DrawLine(pen,x+10,y,x+10,y+4);g.DrawEllipse(pen,x+6,y+8,1,1);g.DrawEllipse(pen,x+14,y+8,1,1);g.DrawLine(pen,x+7,y+13,x+14,y+13);}else if(kind==2){g.DrawRectangle(pen,x+1,y+1,19,7);g.DrawRectangle(pen,x+1,y+12,19,7);g.DrawLine(pen,x+5,y+4,x+8,y+4);g.DrawLine(pen,x+5,y+15,x+8,y+15);}else{g.DrawEllipse(pen,x+2,y+1,6,6);g.DrawEllipse(pen,x+13,y+1,6,6);g.DrawArc(pen,x,y+10,11,12,180,180);g.DrawArc(pen,x+10,y+10,11,12,180,180);}}}
}
}
