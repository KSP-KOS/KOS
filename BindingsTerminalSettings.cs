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
            manager.AddGetter("SESSIONTIME", cpu => cpu.SessionTime);
            manager.AddGetter("VERSION", cpu => Core.VersionInfo);
            manager.AddGetter("VOLUME:NAME", cpu => cpu.SelectedVolume.Name);
            manager.AddSetter("VOLUME:NAME", (cpu, val) => cpu.SelectedVolume.Name = val.ToString());
        }
    }
}
