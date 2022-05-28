using kOS.Safe.Binding;
using kOS.Safe.Encapsulation;

namespace kOS.Binding
{
    [Binding("ksp")]
    public class TerminalSettings : Binding
    {
        private TerminalStruct terminalStructInstance = null;

        public override void AddTo(SharedObjects shared)
        {
            shared.BindingMgr.AddGetter("SESSIONTIME", () => shared.Cpu.SessionTime);
            shared.BindingMgr.AddGetter("VOLUME:NAME", () => shared.VolumeMgr.CurrentVolume.Name);
            shared.BindingMgr.AddGetter("TERMINAL", () => terminalStructInstance ?? (terminalStructInstance = new TerminalStruct(shared)) );
        }
    }
}