using kOS.Safe.Binding;
using kOS.Suffixed;
using kOS.Safe.Encapsulation;

namespace kOS.Binding
{
    [Binding("ksp", "archive")]
    public class DoNothingBinding : Binding
    {
        private NoDelegate doNothingInstance = null;
        public override void AddTo(SharedObjects shared)
        {
            shared.BindingMgr.AddGetter("DONOTHING", () => doNothingInstance ?? (doNothingInstance = new NoDelegate(shared.Cpu)));
        }
    }
}
