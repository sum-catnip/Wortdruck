using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RemoteInstall.Installers;

namespace RemoteInstall {
   class Preset {
      public string Name { get; set; }

      public Wordpress Wordpress { get; set; } = new Wordpress();

      public Preset(string name) { Name = name; }
   }
}
