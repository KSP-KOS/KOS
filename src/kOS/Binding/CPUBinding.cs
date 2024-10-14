using kOS.Lua;
using kOS.Safe.Binding;
using kOS.Module;

namespace kOS.Binding
{
    [Binding("ksp")]
    public class CPUBinding : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            shared.BindingMgr.AddGetter("OPCODESLEFT", () =>
            {
                if (shared.Interpreter is LuaInterpreter)
                {
                    return kOSCustomParameters.Instance.LuaInstructionsPerUpdate - shared.Interpreter.InstructionsThisUpdate();
                }
                return kOSCustomParameters.Instance.InstructionsPerUpdate - shared.Interpreter.InstructionsThisUpdate();
            });
            shared.BindingMgr.MarkVolatile("OPCODESLEFT");
        }
    }
}
