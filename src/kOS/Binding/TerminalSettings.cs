using kOS.Safe.Binding;
using kOS.Safe.Encapsulation;

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
            shared.BindingMgr.AddGetter("TERMINAL", () => new TerminalStruct(shared));
        }
    }
}