using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Net;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.IO.Compression;
using FluentFTP;
using System.Net.Http;

namespace RemoteInstall.Installers {
   public class Wildcards {
      public List<Wildcard> List { get; set; }
      public Wordpress Instance  { get; set; }

      public Wildcards(Wordpress instance) {
         Instance = instance;
         List = new List<Wildcard>();
      }

      public void AddWildcards(IEnumerable<string> names) {
         foreach(string name in names) {
            if (!List.Any(x => x.Name == name))
               List.Add(new Wildcard(name));
         }
      }
   }

   public class Wildcard {
      public string Name  { get; set; }
      public string Value { get; set; }

      public Wildcard(string name) { Name = name; }
   }

   public class Wordpress {
      [Wildcard]
      public string FtpServer { get; set; } = "";
      [Wildcard]
      public string FtpPassword { get; set; } = "";
      [Wildcard]
      public string FtpUsername { get; set; } = "";
      [Wildcard]
      public string TargetPath { get; set; } = "/var/www/html";

      [Wildcard]
      public string WordpressURL      { get; set; } = "https://wordpress.org/latest.zip";
      [Wildcard]
      public string WordpressUsername { get; set; } = "";
      [Wildcard]
      public string WordpressPassword { get; set; } = "";
      [Wildcard]
      public string InstallURL { get; set; } = "http://example.com";
      [Wildcard]
      public string Title { get; set; } = "";
      [Wildcard]
      public string WordpressEmail { get; set; } = "";
      [Wildcard]
      public ObservableCollection<string> Plugins { get; set; } = new ObservableCollection<string>();

      [Wildcard]
      public string DatabaseName { get; set; } = "wordpress";
      [Wildcard]
      public string DatabaseConnection { get; set; } = "localhost";
      [Wildcard]
      public string DatabaseUsername { get; set; } = "";
      [Wildcard]
      public string DatabasePassword { get; set; } = "";
      [Wildcard]
      public string DatabasePrefix { get; set; } = "_wp";

      private static Regex wildcardRegex = new Regex(@"{\s*(?<name>\w+)\s*}", RegexOptions.Compiled);
      private static readonly IEnumerable<PropertyInfo> properties = typeof(Wordpress)
         .GetProperties()
         .Where(prop => prop.IsDefined(typeof(WildcardAttribute), false)
      );

      private void AddWildcards(Wildcards wildcards, string value) {
         wildcards.AddWildcards(
            wildcardRegex
               .Matches(value)
               .Cast<Match>()
               .Select(match => match.Groups["name"].Value)
         );
      }

      public Wildcards GetWildcards() {
         Wildcards wildcards = new Wildcards(this);
         foreach (PropertyInfo prop in properties) {
            switch (prop.GetValue(this)) {
               case string str:
                  AddWildcards(wildcards, str);
                  break;
            }
         }

         return wildcards;
      }
      
      private Wordpress SetWildcards(Wildcards wildcards) {
         Wordpress instance = new Wordpress();

         // copy values over
         foreach (PropertyInfo prop in properties)
            prop.SetValue(instance, prop.GetValue(this));

         // set wildcards with regex replace
         foreach (Wildcard wildcard in wildcards.List) {
            foreach (PropertyInfo prop in properties) {
               switch (prop.GetValue(instance)) {
                  case string str:
                     Regex regex = new Regex(@"{\s*" + wildcard.Name + @"\s*}", RegexOptions.Compiled);
                     prop.SetValue(instance, regex.Replace(str, wildcard.Value));
                     break;
               }
            }
         }

         return instance;
      }

      public static void RecursiveUpload(FtpClient ftp, string localPath, string remotePath) {
         ftp.UploadFiles(Directory.GetFiles(localPath), remotePath);
         foreach(string subdir in Directory.GetDirectories(localPath)) {
            RecursiveUpload(ftp, subdir, remotePath + "/" + new DirectoryInfo(subdir).Name);
         }
      }

      public static void Install(Wildcards wildcards) {
         Wordpress wp = wildcards.Instance.SetWildcards(wildcards);

         // create directory with random name in temp folder
         string wdPath = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;
         string wpzipPath = Path.Combine(Path.Combine(wdPath, "wordpress.zip"));
         string wpFolderPath = Path.Combine(wdPath, "wordpress");
         string pluginsPath = Path.Combine(wpFolderPath, "wp-content", "plugins");
         string wpConfigSample = Path.Combine(wpFolderPath, "wp-config-sample.php");
         string wpConfig = Path.Combine(wpFolderPath, "wp-config.php");
         string keys;

         // download and extract the stuffs
         using (var client = new WebClient()) {
            client.DownloadFile(wp.WordpressURL, wpzipPath);
            ZipFile.ExtractToDirectory(wpzipPath, wdPath);
            foreach (string plugin in wp.Plugins) {
               string targetPath = Path.Combine(wdPath, Path.GetRandomFileName());
               client.DownloadFile(plugin, targetPath);
               ZipFile.ExtractToDirectory(targetPath, pluginsPath);
            }

            keys = client.DownloadString("https://api.wordpress.org/secret-key/1.1/salt/");
         }

         string sampleContent = File.ReadAllText(wpConfigSample);

         sampleContent = sampleContent.Replace("database_name_here", wp.DatabaseName);
         sampleContent = sampleContent.Replace("username_here", wp.DatabaseUsername);
         sampleContent = sampleContent.Replace("password_here", wp.DatabasePassword);
         sampleContent = sampleContent.Replace("localhost", wp.DatabaseConnection);
         sampleContent = sampleContent.Replace("wp_", wp.DatabasePrefix);

         Regex keysRegex = new Regex(@"\/\*\*#@\+.*\/\*\*#@-\*\/", RegexOptions.Singleline);
         sampleContent = keysRegex.Replace(sampleContent, keys);

         File.WriteAllText(wpConfig, sampleContent);

         FtpClient ftp = new FtpClient(wp.FtpServer) {
            Credentials = new NetworkCredential(wp.FtpUsername, wp.FtpPassword)
         };

         ftp.Connect();
         ftp.RetryAttempts = 10;
         RecursiveUpload(ftp, wpFolderPath, wp.TargetPath);
         ftp.Disconnect();

         string installUrl = wp.InstallURL.TrimEnd('/') + "/wp-admin/install.php?step=2";

         using (var http = new HttpClient()) {
            http.PostAsync(installUrl, new FormUrlEncodedContent(
               new KeyValuePair<string, string>[] {
                  new KeyValuePair<string, string>("weblog_title", wp.Title),
                  new KeyValuePair<string, string>("user_name", wp.WordpressUsername),
                  new KeyValuePair<string, string>("admin_password", wp.WordpressPassword),
                  new KeyValuePair<string, string>("pass1-text", wp.WordpressPassword),
                  new KeyValuePair<string, string>("admin_password2", wp.WordpressPassword),
                  new KeyValuePair<string, string>("pw_weak", "on"),
                  new KeyValuePair<string, string>("admin_email", wp.WordpressEmail),
                  new KeyValuePair<string, string>("Submit", "Install+WordPress"),
                  new KeyValuePair<string, string>("language", "")
            })).Wait();

            System.Diagnostics.Process.Start(wp.InstallURL);
         }

         Directory.Delete(wdPath, true);
      }
   }
}
