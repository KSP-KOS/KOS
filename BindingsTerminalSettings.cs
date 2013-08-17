using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kOS
{
    [kOSBinding]
    public class BindingsTerminalSettings : Binding
    {
        public override void AddTo(BindingManager manager)
        {
            //manager.AddGetter("SESSIONTIME", delegate(CPU cpu) { return cpu.SessionTime; });
        }
    }
}
