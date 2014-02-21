using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS
{
    [kOSBinding]
    public class BindingsTerminalSettings : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            _shared = shared;
            _shared.BindingMgr.AddGetter("SESSIONTIME", delegate(CPU cpu) { return cpu.SessionTime; });
            _shared.BindingMgr.AddGetter("VERSION", delegate(CPU cpu) { return Core.VersionInfo; });
        }
    }
}
