using System;
using System.Diagnostics;
using System.IO;

namespace BF4Bots {
 public sealed partial class Central {
  void OpenRecordingReview() {
   string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BF4Central", "Review");
   Directory.CreateDirectory(directory);
   string page = Path.Combine(directory, "revisao.html");
   using (var resource = typeof(Central).Assembly.GetManifestResourceStream("BF4Bots.Review"))
   using (var destination = File.Create(page)) {
    resource.CopyTo(destination);
   }
   Process.Start(new ProcessStartInfo(page) { UseShellExecute = true });
   Status("Revisão de gravações aberta no navegador.");
  }
 }
}
