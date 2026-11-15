using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
namespace BF4Bots {
public static class LanguageTests {
 static void Expect(string pt,string want){string got=L.T(pt);if(got!=want)throw new Exception("Tradução ["+L.Current+"] de '"+pt+"': '"+got+"' (esperado '"+want+"')");}
 static IEnumerable<Control> All(Control c){foreach(Control child in c.Controls){yield return child;foreach(var x in All(child))yield return x;}}
 public static void Run(string root,string folder){
  L.Persist=false;L.Use("pt");Expect("Próximo","Próximo");
  L.Use("en");Expect("Próximo","Next");Expect("Etapa 2 de 5 • 40% concluído","Step 2 of 5 • 40% complete");Expect("Nada foi aplicado ao servidor. A versão da IA mudou. Não vou aplicar perfis sem revisar a compatibilidade.","Nothing was applied to the server. The AI version changed. Profiles will not be applied without a compatibility review.");
  Expect("Difícil • configurado e ativo na partida ✓","Hard • configured and active in the match ✓");Expect("Fácil • não salvo ainda • em uso: Difícil","Easy • not saved yet • in use: Hard");
  L.Use("es");Expect("Próximo","Siguiente");Expect("Noob • não salvo ainda • em uso: Personalizado / original","Novato • aún no guardado • en uso: Personalizado / original");Expect("Escolhido: Siege of Shanghai • Conquista grande","Elegido: Siege of Shanghai • Conquista grande");
  L.Use("pt");
  using(var flags=new Bitmap(3*130+40,110)){using(var g=Graphics.FromImage(flags)){g.Clear(Theme.Background);for(int i=0;i<3;i++){Flags.Draw(g,new Rectangle(20+i*130,15,110,73),L.Codes[i]);}}flags.Save(Path.Combine(folder,"language-flags.png"));}
  var missing=new SortedSet<string>();
  using(var form=new Central(root,false)){
   form.Show();Application.DoEvents();
   var tabs=(TabControl)typeof(Central).GetField("tabs",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(form);
   var picker=All(form).OfType<LanguagePicker>().FirstOrDefault();if(picker==null)throw new Exception("Seletor de idioma ausente");
   foreach(string code in new[]{"en","es"}){
    picker.Choose(code);Application.DoEvents();if(L.Current!=code)throw new Exception("Seletor não trocou o idioma para "+code);
    if(form.Text!=L.T("BF4 • Central local")||tabs.TabPages[0].Text!=L.T("Instalação"))throw new Exception("Janela/abas não traduzidas em "+code);
    if(!All(form).OfType<Label>().Any(x=>x.Text==L.T("01 / Preparar a instalação")))throw new Exception("Título da etapa 1 não traduzido em "+code);
    for(int i=0;i<tabs.TabCount;i++){tabs.SelectedIndex=i;Application.DoEvents();using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(Point.Empty,form.Size));image.Save(Path.Combine(folder,"central-"+code+"-"+i+".png"));}
     foreach(var c in All(tabs.TabPages[i]).Where(x=>x.Visible&&(x is Label||x is ButtonBase))){string t=c.Text;if(t.IndexOfAny("ãçõâêáéíóúÁÉÍÓÚÃÇ".ToCharArray())>=0&&code=="en")missing.Add(t);}}
   }
   picker.Choose("pt");Application.DoEvents();
   picker.Open();Application.DoEvents();if(!picker.IsOpen)throw new Exception("Menu de idiomas não abriu");picker.Close();Application.DoEvents();if(picker.IsOpen)throw new Exception("Menu de idiomas não fechou");
   using(var shot=new Form{ClientSize=new Size(520,190),BackColor=Theme.Ink,StartPosition=FormStartPosition.Manual,Location=new Point(-3000,-3000),ShowInTaskbar=false}){var closed=new LanguagePicker{Location=new Point(20,20)};var list=new LanguageMenu(closed){Location=new Point(250,20),Width=240};shot.Controls.Add(closed);shot.Controls.Add(list);shot.Show();list.Reset();Application.DoEvents();using(var image=new Bitmap(shot.ClientSize.Width,shot.ClientSize.Height)){shot.DrawToBitmap(image,new Rectangle(Point.Empty,shot.ClientSize));image.Save(Path.Combine(folder,"language-picker.png"));}shot.Close();}
   if(tabs.TabPages[0].Text!="Instalação"||!All(form).OfType<Label>().Any(x=>x.Text=="01 / Preparar a instalação"))throw new Exception("Voltar ao português não restaurou os textos");
   form.Close();
  }
  File.WriteAllLines(Path.Combine(folder,"language-missing.txt"),missing,Encoding.UTF8);
 }
}
}
