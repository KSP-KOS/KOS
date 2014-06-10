using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Execution;

namespace kOS.Binding
{
    [kOSBinding]
    public class TerminalSettings : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            Shared = shared;
            Shared.BindingMgr.AddGetter("SESSIONTIME", cpu => cpu.SessionTime);
            Shared.BindingMgr.AddGetter("VERSION", cpu => Core.VersionInfo);
            Shared.BindingMgr.AddGetter("VOLUME:NAME", cpu => Shared.VolumeMgr.CurrentVolume.Name);
        }
    }
}