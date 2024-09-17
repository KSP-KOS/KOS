using kOS.Safe.Binding;
using kOS.Module;

namespace kOS.Binding
{
    [Binding("ksp")]
    public class CPUBinding : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            shared.BindingMgr.AddGetter("OPCODESLEFT", delegate { return kOSCustomParameters.Instance.InstructionsPerUpdate - shared.Interpreter.InstructionsThisUpdate(); });
            shared.BindingMgr.MarkVolatile("OPCODESLEFT");
        }
    }
}
