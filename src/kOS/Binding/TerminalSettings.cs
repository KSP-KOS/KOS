using kOS.Safe.Binding;

namespace kOS.Binding
{
    [Binding]
    public class TerminalSettings : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            shared.BindingMgr.AddGetter("SESSIONTIME", () => shared.Cpu.SessionTime);
            shared.BindingMgr.AddGetter("VERSION", () => Core.VersionInfo);
            shared.BindingMgr.AddGetter("VOLUME:NAME", () => shared.VolumeMgr.CurrentVolume.Name);
        }
    }
}