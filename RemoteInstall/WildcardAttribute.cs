using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteInstall {
   [AttributeUsage(AttributeTargets.Property)]
   class WildcardAttribute : Attribute {}
}
