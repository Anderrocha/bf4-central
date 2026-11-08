using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Resources;
using System.Windows.Forms;

namespace BF4Bots {
public sealed class MapArtwork : IDisposable {
 readonly Dictionary<string,byte[]> data=new Dictionary<string,byte[]>(StringComparer.OrdinalIgnoreCase);
 public readonly ImageList Thumbnails=new ImageList{ColorDepth=ColorDepth.Depth32Bit,ImageSize=new Size(96,48),TransparentColor=Color.Transparent};
 public MapArtwork(){using(var stream=typeof(MapArtwork).Assembly.GetManifestResourceStream("BF4Bots.MapArt.resources")){if(stream!=null)using(var reader=new ResourceReader(stream))foreach(DictionaryEntry item in reader){var bytes=item.Value as byte[];if(bytes!=null&&bytes.Length<=2000000)data[Convert.ToString(item.Key)]=bytes;}}}
 public Image Load(string id){byte[] bytes;if(!data.TryGetValue(id,out bytes))return null;try{using(var stream=new MemoryStream(bytes))using(var source=Image.FromStream(stream)){if(source.Width>4096||source.Height>4096)return null;return new Bitmap(source);}}catch(ArgumentException){return null;}}
 public string Thumbnail(string id){if(Thumbnails.Images.ContainsKey(id))return id;using(var source=Load(id))using(var thumbnail=new Bitmap(96,48)){using(var g=Graphics.FromImage(thumbnail)){g.Clear(Theme.Ink);if(source!=null)DrawCover(g,source,new Rectangle(0,0,96,48));else using(var pen=new Pen(Theme.Muted)){g.DrawRectangle(pen,8,8,79,31);g.DrawLine(pen,8,39,40,18);g.DrawLine(pen,40,18,70,39);}}Thumbnails.Images.Add(id,thumbnail);var nativeHandle=Thumbnails.Handle;}return id;}
 public static void DrawCover(Graphics graphics,Image image,Rectangle bounds){float scale=Math.Max((float)bounds.Width/image.Width,(float)bounds.Height/image.Height);float width=bounds.Width/scale,height=bounds.Height/scale;graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;graphics.DrawImage(image,bounds,new RectangleF((image.Width-width)/2,(image.Height-height)/2,width,height),GraphicsUnit.Pixel);}
 public void Dispose(){Thumbnails.Dispose();data.Clear();}
}
public sealed class MapPreview : Control {
 Image image;string mapName="Escolha seu próximo mapa",modeName="Selecione um mapa na lista",support="";
 public MapPreview(){DoubleBuffered=true;BackColor=Theme.Ink;ForeColor=Color.White;Dock=DockStyle.Fill;AccessibleName="Prévia do mapa selecionado";Margin=new Padding(0,0,0,12);}
 public void SetMap(Image next,string name,string mode,bool supported){if(image!=null)image.Dispose();image=next;mapName=name;modeName=mode;support=supported?"SUPORTE A BOTS DECLARADO":"BOTS NÃO CONFIRMADOS NESTE MODO";AccessibleName="Mapa selecionado: "+name+". "+mode+". "+support;Invalidate();}
 public void Clear(){if(image!=null)image.Dispose();image=null;mapName="Nenhum mapa compatível neste modo";modeName="Marque Mostrar todos para explorar o catálogo";support="";Invalidate();}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(Width<1||Height<1)return;var g=e.Graphics;g.Clear(Theme.Ink);if(image!=null)MapArtwork.DrawCover(g,image,ClientRectangle);using(var shade=new LinearGradientBrush(ClientRectangle,Color.FromArgb(15,0,0,0),Color.FromArgb(235,10,23,31),90))g.FillRectangle(shade,ClientRectangle);using(var small=new Font(Theme.Body.FontFamily,8.5f,FontStyle.Bold))using(var title=new Font(Theme.Heading.FontFamily,19,FontStyle.Bold)){TextRenderer.DrawText(g,support,small,new Rectangle(16,16,Width-32,22),Color.White,TextFormatFlags.EndEllipsis);TextRenderer.DrawText(g,mapName,title,new Rectangle(14,Height-75,Width-28,35),Color.White,TextFormatFlags.EndEllipsis);TextRenderer.DrawText(g,modeName,Theme.Body,new Rectangle(16,Height-37,Width-32,25),Color.FromArgb(220,230,237),TextFormatFlags.EndEllipsis);}if(image==null&&support.Length>0)TextRenderer.DrawText(g,"PRÉVIA INDISPONÍVEL",Theme.Body,new Rectangle(16,Height/2-20,Width-32,25),Color.FromArgb(170,190,203),TextFormatFlags.EndEllipsis);}
 protected override void Dispose(bool disposing){if(disposing&&image!=null){image.Dispose();image=null;}base.Dispose(disposing);}
}
}
