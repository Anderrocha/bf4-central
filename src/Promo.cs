using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace BF4Bots {
// Cartão do projeto Sócio Premiado na tela final: imagem, convite, link e botão para o site.
public sealed class PromoCard : Panel {
 public const string Url="https://sociopremiado.com.br/";
 static readonly Color Green=ColorTranslator.FromHtml("#00A653"),DarkGreen=ColorTranslator.FromHtml("#0B7A43");
 readonly Image art;
 public PromoCard(){
  DoubleBuffered=true;BackColor=Theme.Background;Padding=new Padding(14,10,16,10);
  using(var s=typeof(PromoCard).Assembly.GetManifestResourceStream("BF4Bots.Promo")){if(s!=null)try{art=new Bitmap(Image.FromStream(s));}catch{art=null;}}
  var grid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=3,BackColor=Theme.Surface,Margin=Padding.Empty};
  grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,112));grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,190));
  grid.RowStyles.Add(new RowStyle(SizeType.Percent,40));grid.RowStyles.Add(new RowStyle(SizeType.Percent,32));grid.RowStyles.Add(new RowStyle(SizeType.Percent,28));
  var picture=new RoundPicture{Image=art,Dock=DockStyle.Fill,Margin=new Padding(0,0,12,0),Cursor=Cursors.Hand,AccessibleName="Sócio Premiado"};picture.Click+=(s,e)=>Open();grid.Controls.Add(picture,0,0);grid.SetRowSpan(picture,3);
  grid.Controls.Add(new Label{Text="Sócio Premiado",Font=new Font("Bahnschrift",15,FontStyle.Bold),ForeColor=DarkGreen,Dock=DockStyle.Fill,TextAlign=ContentAlignment.BottomLeft,AutoEllipsis=true},1,0);
  grid.Controls.Add(new Label{Text="Ajude o projeto a evoluir: participe e concorra a prêmios.",ForeColor=Theme.Ink,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true},1,1);
  var link=new LinkLabel{Text="sociopremiado.com.br",Dock=DockStyle.Fill,TextAlign=ContentAlignment.TopLeft,LinkColor=Green,ActiveLinkColor=DarkGreen,VisitedLinkColor=DarkGreen,LinkBehavior=LinkBehavior.HoverUnderline,AccessibleName="Abrir sociopremiado.com.br"};link.LinkClicked+=(s,e)=>Open();grid.Controls.Add(link,1,2);
  var go=new ActionButton{Text="Ir para o site",BackColor=Green,ForeColor=Color.White,Width=176,Height=40,Anchor=AnchorStyles.Right,Margin=Padding.Empty};go.Click+=(s,e)=>Open();grid.Controls.Add(go,2,0);grid.SetRowSpan(go,3);
  Controls.Add(grid);
  Resize+=(s,e)=>Invalidate();
 }
 static void Open(){try{Process.Start(new ProcessStartInfo(Url){UseShellExecute=true});}catch{}}
 // Fundo branco com cantos arredondados e borda verde suave.
 protected override void OnPaint(PaintEventArgs e){
  base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
  var r=new Rectangle(0,0,Width-1,Height-1);
  using(var path=Round(r,14))using(var fill=new SolidBrush(Theme.Surface))using(var pen=new Pen(Color.FromArgb(120,Green),1.5f)){e.Graphics.FillPath(fill,path);e.Graphics.DrawPath(pen,path);}
 }
 internal static GraphicsPath Round(Rectangle r,int radius){var p=new GraphicsPath();int d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
 protected override void Dispose(bool disposing){if(disposing&&art!=null)art.Dispose();base.Dispose(disposing);}
}
sealed class RoundPicture : Control {
 public Image Image;
 public RoundPicture(){SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.UserPaint|ControlStyles.ResizeRedraw,true);BackColor=Theme.Surface;}
 protected override void OnPaint(PaintEventArgs e){
  e.Graphics.Clear(BackColor);if(Image==null)return;e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;e.Graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;
  int side=Math.Min(Width,Height);var box=new Rectangle(0,(Height-side)/2,side,side);
  using(var path=PromoCard.Round(box,12)){e.Graphics.SetClip(path);e.Graphics.DrawImage(Image,box);e.Graphics.ResetClip();}
 }
}
}
