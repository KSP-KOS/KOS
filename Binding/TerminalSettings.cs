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
            _shared = shared;
            _shared.BindingMgr.AddGetter("SESSIONTIME", cpu => cpu.SessionTime);
            _shared.BindingMgr.AddGetter("VERSION", cpu => Core.VersionInfo);
            _shared.BindingMgr.AddGetter("VOLUME:NAME", cpu => _shared.VolumeMgr.CurrentVolume.Name);
        }
    }
}