using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Newtonsoft.Json;

using RemoteInstall.Installers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;

namespace RemoteInstall {
   class ViewModel : INotifyPropertyChanged {
      private const string PresetPath = "presets.json";

      public event PropertyChangedEventHandler PropertyChanged;
      private Preset m_selected;
      private string m_selectedPlugin;

      public ObservableCollection<Preset> Presets { get; set; }

      public string SelectedPlugin {
         get { return m_selectedPlugin; }
         set {
            m_selectedPlugin = value;
            NotifyPropertyChanged();
         }
      }

      public Preset Selected {
         get { return m_selected; }
         set {
            m_selected = value;
            NotifyPropertyChanged();
            NotifyPropertyChanged(nameof(HasSelected));
         }
      }

      public bool HasSelected {
         get { return Selected != null; }
      }

      public ViewModel() {
         // tryna load presets
         try {
            string presetsString = File.ReadAllText(PresetPath);
            Presets = JsonConvert.DeserializeObject<ObservableCollection<Preset>>(presetsString);
         }
         catch(IOException) {
            // lets just make an empty one
            // json exceptions should be caught tho
            Presets = new ObservableCollection<Preset>();
         }
      }

      public void AddPlugin(string pluginURL) { if (!Selected.Wordpress.Plugins.Contains(pluginURL)) Selected?.Wordpress.Plugins.Add(pluginURL); }
      public void RemovePlugin()         { Selected.Wordpress.Plugins.Remove(SelectedPlugin); }
      public void AddPreset(string name) { Presets.Add(new Preset(name)); }
      public void RemovePreset()         { Presets.Remove(Selected);      }

      /// <summary>
      /// saving presets to disk as serialized json string
      /// </summary>
      /// <exception cref="IOException">failed to write file :(</exception>
      public void SavePresets() {
         string serialized = JsonConvert.SerializeObject(Presets, Formatting.Indented);
         File.WriteAllText(PresetPath, serialized);
      }

      private void NotifyPropertyChanged([CallerMemberName] string info = "") {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
      }
   }
}
