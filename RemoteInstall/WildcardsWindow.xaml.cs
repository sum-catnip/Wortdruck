using MahApps.Metro.Controls;
using RemoteInstall.Installers;
using System.Windows;
using System.Threading;
using System;
using System.IO;

namespace RemoteInstall {
   /// <summary>
   /// Interaction logic for WildcardsWindow.xaml
   /// </summary>
   public partial class WildcardsWindow : MetroWindow {
      private Wildcards m_wildcards;

      public WildcardsWindow(Wildcards wildcards) {
         m_wildcards = wildcards;
         DataContext = m_wildcards;
         InitializeComponent();
      }

      private void OKClick(object sender, RoutedEventArgs e) {
         Progress.IsActive = true;
         IsEnabled = false;
         new Thread(() => {
            try { Wordpress.Install(m_wildcards); }
            catch(Exception ex) {
               string filename = DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + ".err";
               MessageBox.Show("Installation failed. error report will be written to " + filename, "ERROR");
               File.WriteAllText(filename, ex.ToString());
            }
            finally { Application.Current.Dispatcher.Invoke(() => Close()); }
         }).Start();
      }

      private void CancelClick(object sender, RoutedEventArgs e) {
         Close();
      }
   }
}
