using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS
{
    [kOSBinding]
    public class BindingsTerminalSettings : Binding
    {
        public override void AddTo(BindingManager manager)
        {
            manager.AddGetter("SESSIONTIME", delegate(CPU cpu) { return cpu.SessionTime; });
            manager.AddGetter("VERSION", delegate(CPU cpu) { return Core.VersionInfo; });
        }
    }
}
