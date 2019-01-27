using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace RemoteInstall {
   public partial class MainWindow : MetroWindow {
      private ViewModel m_viewModel = new ViewModel();

      public MainWindow() {
         DataContext = m_viewModel;
         InitializeComponent();
      }

      protected override void OnClosing(CancelEventArgs e) {
         base.OnClosing(e);
         m_viewModel.SavePresets();
      }

      private void AddPresetClick(object sender, RoutedEventArgs e) {
         this.ShowInputAsync("Add Preset", "Enter a name for the new preset").ContinueWith((result) => {
            Application.Current.Dispatcher.Invoke(new Action(() => { m_viewModel.AddPreset(result.Result); }));
         });
      }

      private void RemovePresetClick(object sender, RoutedEventArgs e) {
         m_viewModel.RemovePreset();
      }

      private void AddPluginClick(object sender, RoutedEventArgs e) {
         this.ShowInputAsync("Add Plugin", "enter the direct url for the plugin").ContinueWith((result) => {
            Application.Current.Dispatcher.Invoke(new Action(() => { m_viewModel.AddPlugin(result.Result); }));
         });
      }

      private void RemovePluginClick(object sender, RoutedEventArgs e) {
         m_viewModel.RemovePlugin();
      }

      private void InstallClick(object sender, RoutedEventArgs e) {
         if(m_viewModel.HasSelected) new WildcardsWindow(m_viewModel.Selected.Wordpress.GetWildcards()).Show();
      }
   }
}
