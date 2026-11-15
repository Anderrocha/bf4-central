using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace BF4Bots {

// Idioma da interface. Os textos de origem continuam em português no código (as etapas ✓ dependem deles);
// a tradução acontece só na hora de exibir.
public static partial class L {
 public static readonly string[] Codes={"pt","en","es"};
 static string current="pt";
 public static string Current{get{return current;}}
 public static event Action Changed;
 static readonly Dictionary<string,string[]> exact=new Dictionary<string,string[]>(StringComparer.Ordinal);
 static readonly List<KeyValuePair<string,string[]>> fragments=new List<KeyValuePair<string,string[]>>();
 static readonly Dictionary<string,string> cache=new Dictionary<string,string>(StringComparer.Ordinal);
 static string FilePath{get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BF4BotLauncher","language.txt");}}
 static L(){
  for(int i=0;i+2<Table.Length;i+=3){if(!exact.ContainsKey(Table[i]))exact.Add(Table[i],new[]{Table[i+1],Table[i+2]});}
  var extra=new HashSet<string>(Fragments,StringComparer.Ordinal);
  foreach(var kv in exact)if(extra.Contains(kv.Key)||(kv.Key.Length>=6&&kv.Key.IndexOfAny(new[]{' ','.',':',',',';','•','…','?'})>=0))fragments.Add(kv);
  fragments.Sort((a,b)=>b.Key.Length.CompareTo(a.Key.Length));
 }
 public static string NativeName(string code){return code=="en"?"English":code=="es"?"Español":"Português (Brasil)";}
 public static int Entries{get{return exact.Count;}}
 public static bool Known(string pt){return exact.ContainsKey(pt);}
 // Carrega o idioma salvo; sem arquivo, segue o idioma do Windows.
 public static void Load(){string code=null;try{if(File.Exists(FilePath))code=File.ReadAllText(FilePath).Trim().ToLowerInvariant();}catch(IOException){}catch(UnauthorizedAccessException){}if(!Codes.Contains(code)){string ui=CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;code=ui=="pt"?"pt":ui=="es"?"es":ui=="en"?"en":"pt";}Use(code);}
 // Troca sem gravar (usado pelos testes).
 public static void Use(string code){if(!Codes.Contains(code))code="pt";if(code==current)return;current=code;cache.Clear();var h=Changed;if(h!=null)h();}
 public static bool Persist=true;
 public static void Set(string code){Use(code);if(!Persist)return;try{Directory.CreateDirectory(Path.GetDirectoryName(FilePath));File.WriteAllText(FilePath,current);}catch(IOException){}catch(UnauthorizedAccessException){}}
 public static string T(string pt){
  if(String.IsNullOrEmpty(pt)||current=="pt")return pt;
  int i=current=="en"?0:1;string[] v;
  if(exact.TryGetValue(pt,out v))return v[i];
  string done;if(cache.TryGetValue(pt,out done))return done;
  string r=pt;foreach(var f in fragments)if(r.IndexOf(f.Key,StringComparison.Ordinal)>=0)r=r.Replace(f.Key,f.Value[i]);
  if(cache.Count>4000)cache.Clear();cache[pt]=r;return r;
 }
 // ComboBox com itens em português: desenha cada item traduzido.
 public static void Translate(ComboBox box){box.DrawMode=DrawMode.OwnerDrawFixed;box.DrawItem+=(s,e)=>{e.DrawBackground();string text=e.Index>=0?Convert.ToString(box.Items[e.Index]):Convert.ToString(box.SelectedItem);bool selected=(e.State&DrawItemState.Selected)!=0;TextRenderer.DrawText(e.Graphics,T(text),box.Font,e.Bounds,selected?SystemColors.HighlightText:Theme.Ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);e.DrawFocusRectangle();};}
}

// Aplica L.T aos textos de rótulos, caixas de seleção, abas e janelas, e acompanha mudanças feitas depois.
public static class Localizer {
 static readonly Dictionary<Control,string> source=new Dictionary<Control,string>();
 static readonly Dictionary<ColumnHeader,string> headers=new Dictionary<ColumnHeader,string>();
 static readonly HashSet<Control> hooked=new HashSet<Control>();
 static readonly List<Control> roots=new List<Control>();
 static bool applying,subscribed;
 public static void Attach(Control root){if(!subscribed){subscribed=true;L.Changed+=RefreshAll;}if(!roots.Contains(root)){roots.Add(root);root.Disposed+=(s,e)=>roots.Remove(root);}Hook(root);}
 static bool Translatable(Control c){return !(c is TextBoxBase||c is ComboBox||c is UpDownBase||c is ListBox||c is ActionButton||c is StatusBar||c is TabControl||c is MapPreview||c is WebBrowser||c is ListView);}
 static void Hook(Control c){
  if(!hooked.Add(c))return;
  c.Disposed+=OnGone;
  var list=c as ListView;if(list!=null)foreach(ColumnHeader h in list.Columns){if(!headers.ContainsKey(h))headers[h]=h.Text;h.Text=L.T(headers[h]);}
  if(c is UpDownBase||c is TextBoxBase||c is ComboBox)return;
  if(Translatable(c)){source[c]=c.Text;c.TextChanged+=OnText;Apply(c);}
  c.ControlAdded+=OnAdded;
  foreach(Control child in c.Controls)Hook(child);
 }
 static void OnAdded(object s,ControlEventArgs e){Hook(e.Control);}
 static void OnGone(object s,EventArgs e){var c=(Control)s;source.Remove(c);hooked.Remove(c);var list=c as ListView;if(list!=null)foreach(ColumnHeader h in list.Columns)headers.Remove(h);}
 static void OnText(object s,EventArgs e){if(applying)return;var c=(Control)s;source[c]=c.Text;Apply(c);}
 static void Apply(Control c){string original;if(!source.TryGetValue(c,out original))return;string t=L.T(original);if(c.Text!=t){applying=true;try{c.Text=t;}finally{applying=false;}}}
 static void RefreshAll(){foreach(var c in source.Keys.ToList())if(!c.IsDisposed)Apply(c);foreach(var kv in headers.ToList())kv.Key.Text=L.T(kv.Value);foreach(var r in roots.ToList())if(!r.IsDisposed){r.Invalidate(true);r.PerformLayout();}}
}

// Bandeiras desenhadas em código (sem imagens externas).
public static class Flags {
 public static void Draw(Graphics g,Rectangle r,string code,float radius=0){
  var mode=g.SmoothingMode;g.SmoothingMode=SmoothingMode.AntiAlias;float w=r.Width,h=r.Height;
  var outer=g.Save();GraphicsPath round=null;if(radius>0){round=ActionButton.Round(new RectangleF(r.X,r.Y,r.Width,r.Height),radius);g.SetClip(round,CombineMode.Intersect);}
  if(code=="en"){
   float stripe=h/13f;for(int i=0;i<13;i++)using(var b=new SolidBrush(i%2==0?Color.FromArgb(178,34,52):Color.White))g.FillRectangle(b,r.X,r.Y+stripe*i,w,stripe+0.5f);
   var canton=new RectangleF(r.X,r.Y,w*0.42f,stripe*7);using(var b=new SolidBrush(Color.FromArgb(60,59,110)))g.FillRectangle(b,canton);
   int cols=5,rows=4;float dot=Math.Max(1f,h/16f);for(int y=0;y<rows;y++)for(int x=0;x<cols;x++){float px=canton.X+canton.Width*(x+0.5f)/cols-dot/2,py=canton.Y+canton.Height*(y+0.5f)/rows-dot/2;g.FillEllipse(Brushes.White,px,py,dot,dot);}
  }else if(code=="es"){
   using(var red=new SolidBrush(Color.FromArgb(198,11,30)))g.FillRectangle(red,r);
   using(var yellow=new SolidBrush(Color.FromArgb(255,196,0)))g.FillRectangle(yellow,r.X,r.Y+h/4f,w,h/2f);
  }else{
   using(var green=new SolidBrush(Color.FromArgb(0,156,59)))g.FillRectangle(green,r);
   float mx=w*0.09f,my=h*0.12f,cx=r.X+w/2f,cy=r.Y+h/2f;
   using(var yellow=new SolidBrush(Color.FromArgb(255,223,0)))g.FillPolygon(yellow,new[]{new PointF(cx,r.Y+my),new PointF(r.Right-mx,cy),new PointF(cx,r.Bottom-my),new PointF(r.X+mx,cy)});
   float rad=h*0.27f;var disk=new RectangleF(cx-rad,cy-rad,rad*2,rad*2);using(var blue=new SolidBrush(Color.FromArgb(0,39,118)))g.FillEllipse(blue,disk);
   using(var clip=new GraphicsPath()){clip.AddEllipse(disk);var state=g.Save();g.SetClip(clip);float big=rad*2.05f,ox=cx-rad*0.3f,oy=cy+rad*2f;using(var band=new Pen(Color.White,Math.Max(1f,h/18f)))g.DrawEllipse(band,ox-big,oy-big,big*2,big*2);g.Restore(state);}
  }
  g.Restore(outer);
  using(var border=new Pen(Color.FromArgb(70,0,0,0))){if(round!=null){using(var edge=ActionButton.Round(new RectangleF(r.X+0.5f,r.Y+0.5f,r.Width-1,r.Height-1),radius))g.DrawPath(border,edge);round.Dispose();}else g.DrawRectangle(border,r.X,r.Y,r.Width-1,r.Height-1);}
  g.SmoothingMode=mode;
 }
}

// Seletor de idioma no cabeçalho: pílula arredondada no estilo dos botões da Central
// (mouse por cima, clique âmbar, aberto) e menu suspenso arredondado com destaque e ✓.
public sealed class LanguagePicker : Control {
 bool hover,pressed,open;readonly Timer anim=new Timer{Interval=15};float turn;ToolStripDropDown drop;LanguageMenu menu;DateTime closedAt;
 public LanguagePicker(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw|ControlStyles.SupportsTransparentBackColor|ControlStyles.Selectable,true);BackColor=Color.Transparent;Size=new Size(200,38);Font=Theme.Body;Cursor=Cursors.Hand;TabStop=true;AccessibleRole=AccessibleRole.ComboBox;AccessibleName="Idioma / Language";AccessibleDescription=L.NativeName(L.Current);L.Changed+=Sync;anim.Tick+=(s,e)=>{float target=open?1f:0f;turn+=(target-turn)*0.35f;if(Math.Abs(target-turn)<0.02f){turn=target;anim.Stop();}Invalidate();};}
 public string Value{get{return L.Current;}}
 public bool IsOpen{get{return open;}}
 public void Choose(string code){L.Set(code);Close();}
 void Sync(){if(IsDisposed)return;AccessibleDescription=L.NativeName(L.Current);Invalidate();if(menu!=null)menu.Invalidate();}
 void Animate(){anim.Start();Invalidate();}
 public void Open(){
  if(open)return;
  if(drop==null){menu=new LanguageMenu(this){Width=Math.Max(Width,240)};var host=new ToolStripControlHost(menu){Padding=Padding.Empty,Margin=Padding.Empty,AutoSize=false,Size=menu.Size};drop=new ToolStripDropDown{Padding=Padding.Empty,Margin=Padding.Empty,AutoSize=false,DropShadowEnabled=false,Size=menu.Size,BackColor=Theme.Surface};drop.Items.Add(host);using(var path=ActionButton.Round(new RectangleF(0,0,menu.Width,menu.Height),10))drop.Region=new Region(path);drop.Closed+=(s,e)=>{open=false;closedAt=DateTime.Now;Animate();};}
  menu.Reset();open=true;Animate();drop.Show(this,new Point(Width-menu.Width,Height+6));menu.Focus();
 }
 public void Close(){if(drop!=null&&drop.Visible)drop.Close();}
 protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
 protected override void OnMouseLeave(EventArgs e){hover=false;pressed=false;Invalidate();base.OnMouseLeave(e);}
 protected override void OnMouseDown(MouseEventArgs e){if(e.Button==MouseButtons.Left){pressed=true;Focus();Invalidate();}base.OnMouseDown(e);}
 protected override void OnMouseUp(MouseEventArgs e){bool click=pressed&&ClientRectangle.Contains(e.Location);pressed=false;Invalidate();base.OnMouseUp(e);
  // O clique que fecha o menu também chega aqui; não reabrir logo em seguida.
  if(click&&!open&&(DateTime.Now-closedAt).TotalMilliseconds>250)Open();}
 protected override bool IsInputKey(Keys k){return k==Keys.Up||k==Keys.Down||base.IsInputKey(k);}
 protected override void OnKeyDown(KeyEventArgs e){int i=Array.IndexOf(L.Codes,L.Current);if(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Space||(e.Alt&&e.KeyCode==Keys.Down)){Open();e.Handled=true;}else if(e.KeyCode==Keys.Down&&i<L.Codes.Length-1){L.Set(L.Codes[i+1]);e.Handled=true;}else if(e.KeyCode==Keys.Up&&i>0){L.Set(L.Codes[i-1]);e.Handled=true;}base.OnKeyDown(e);}
 protected override void OnGotFocus(EventArgs e){Invalidate();base.OnGotFocus(e);}
 protected override void OnLostFocus(EventArgs e){Invalidate();base.OnLostFocus(e);}
 protected override void OnPaint(PaintEventArgs e){
  var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
  Color fill=pressed?Theme.Pressed:open?Theme.Pressed:hover?Theme.Hover:Theme.Surface,border=pressed||open?Theme.Accent:hover?Theme.Ink:Theme.Border;
  var r=new RectangleF(0.5f,0.5f,Width-1.5f,Height-1.5f);if(pressed)r.Offset(0,1);
  using(var path=ActionButton.Round(r,Height/2f-1))using(var b=new SolidBrush(fill))using(var p=new Pen(border,pressed||open?1.6f:1f)){g.FillPath(b,path);g.DrawPath(p,path);}
  if(Focused&&ShowFocusCues&&!open)using(var path=ActionButton.Round(RectangleF.Inflate(r,-3,-3),Height/2f-4))using(var p=new Pen(Theme.Accent,1.4f))g.DrawPath(p,path);
  int dy=pressed?1:0;var flag=new Rectangle(14,(Height-16)/2+dy,24,16);Flags.Draw(g,flag,L.Current,3);
  TextRenderer.DrawText(g,L.NativeName(L.Current),Font,new Rectangle(flag.Right+10,dy,Math.Max(1,Width-flag.Right-44),Height),Theme.Ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine);
  // Seta que gira para cima quando o menu abre.
  float cx=Width-22,cy=Height/2f+dy,s=4.5f;var state=g.Save();g.TranslateTransform(cx,cy);g.RotateTransform(180f*turn);using(var p=new Pen(open||pressed?Theme.Accent:Theme.Ink,2f){StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round})g.DrawLines(p,new[]{new PointF(-s,-s/2),new PointF(0,s/2),new PointF(s,-s/2)});g.Restore(state);
 }
 protected override void Dispose(bool disposing){if(disposing){L.Changed-=Sync;anim.Dispose();if(drop!=null)drop.Dispose();}base.Dispose(disposing);}
}

// Lista do menu de idiomas: linhas arredondadas, destaque ao passar o mouse, idioma atual em âmbar com ✓.
public sealed class LanguageMenu : Control {
 readonly LanguagePicker owner;int hot=-1,down=-1;const int Row=36,Pad=6;
 public LanguageMenu(LanguagePicker picker){owner=picker;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw|ControlStyles.Selectable,true);Font=Theme.Body;Height=Pad*2+Row*L.Codes.Length;Cursor=Cursors.Hand;AccessibleRole=AccessibleRole.List;}
 public void Reset(){hot=Array.IndexOf(L.Codes,L.Current);down=-1;Invalidate();}
 int At(Point p){int i=(p.Y-Pad)/Row;return p.Y>=Pad&&i>=0&&i<L.Codes.Length?i:-1;}
 protected override void OnMouseMove(MouseEventArgs e){int i=At(e.Location);if(i!=hot){hot=i;Invalidate();}base.OnMouseMove(e);}
 protected override void OnMouseDown(MouseEventArgs e){down=At(e.Location);Invalidate();base.OnMouseDown(e);}
 protected override void OnMouseUp(MouseEventArgs e){int i=At(e.Location);if(i>=0&&i==down)owner.Choose(L.Codes[i]);down=-1;Invalidate();base.OnMouseUp(e);}
 protected override bool IsInputKey(Keys k){return k==Keys.Up||k==Keys.Down||k==Keys.Enter||base.IsInputKey(k);}
 protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Down){hot=Math.Min(L.Codes.Length-1,hot+1);Invalidate();}else if(e.KeyCode==Keys.Up){hot=Math.Max(0,hot-1);Invalidate();}else if((e.KeyCode==Keys.Enter||e.KeyCode==Keys.Space)&&hot>=0)owner.Choose(L.Codes[hot]);else if(e.KeyCode==Keys.Escape)owner.Close();base.OnKeyDown(e);}
 protected override void OnPaint(PaintEventArgs e){
  var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Theme.Surface);
  for(int i=0;i<L.Codes.Length;i++){
   string code=L.Codes[i];bool current=code==L.Current;var row=new RectangleF(Pad,Pad+i*Row+1,Width-Pad*2,Row-2);
   Color? fill=i==down?Theme.Pressed:current?Color.FromArgb(251,240,231):i==hot?Theme.Hover:(Color?)null;
   if(fill.HasValue)using(var path=ActionButton.Round(row,7))using(var b=new SolidBrush(fill.Value))g.FillPath(b,path);
   if(i==hot&&!current&&i!=down)using(var path=ActionButton.Round(row,7))using(var p=new Pen(Theme.Border))g.DrawPath(p,path);
   if(current)using(var b=new SolidBrush(Theme.Accent))using(var path=ActionButton.Round(new RectangleF(row.X+3,row.Y+9,3,row.Height-18),1.5f))g.FillPath(b,path);
   var flag=new Rectangle((int)row.X+14,(int)(row.Y+(row.Height-16)/2),24,16);Flags.Draw(g,flag,code,3);
   using(var font=new Font(Font,current?FontStyle.Bold:FontStyle.Regular))TextRenderer.DrawText(g,L.NativeName(code),font,new Rectangle(flag.Right+10,(int)row.Y,Math.Max(1,(int)row.Right-flag.Right-38),(int)row.Height),current?Theme.Accent:Theme.Ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine);
   if(current){float cx=row.Right-18,cy=row.Y+row.Height/2;using(var p=new Pen(Theme.Accent,2f){StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round})g.DrawLines(p,new[]{new PointF(cx-5,cy),new PointF(cx-1.5f,cy+3.5f),new PointF(cx+5,cy-4)});}
  }
  using(var path=ActionButton.Round(new RectangleF(0.5f,0.5f,Width-1.5f,Height-1.5f),10))using(var p=new Pen(Theme.Border,1.2f))g.DrawPath(p,path);
 }
}
}
