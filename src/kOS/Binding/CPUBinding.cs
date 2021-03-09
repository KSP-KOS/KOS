using kOS.Safe.Binding;
using kOS.Module;

namespace kOS.Binding
{
    [Binding("ksp")]
    public class CPUBinding : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            shared.BindingMgr.AddGetter("TICKSLEFT", delegate { return kOSCustomParameters.Instance.InstructionsPerUpdate - shared.Cpu.InstructionsThisUpdate; });
        }
    }
}
