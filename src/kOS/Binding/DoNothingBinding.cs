using kOS.Safe.Binding;
using kOS.Suffixed;
using kOS.Safe.Encapsulation;

namespace kOS.Binding
{
    [Binding("ksp")]
    public class DoNothingBinding : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            shared.BindingMgr.AddGetter("DONOTHING", () => new NoDelegate(shared.Cpu));
        }
    }
}
