using kOS.Safe.Binding;

namespace kOS.Binding
{
    [kOSBinding]
    public class TerminalSettings : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            shared.BindingMgr.AddGetter("SESSIONTIME", cpu => cpu.SessionTime);
            shared.BindingMgr.AddGetter("VERSION", cpu => Core.VersionInfo);
            shared.BindingMgr.AddGetter("VOLUME:NAME", cpu => shared.VolumeMgr.CurrentVolume.Name);
        }
    }
}