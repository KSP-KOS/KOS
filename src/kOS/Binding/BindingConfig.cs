using kOS.Safe.Binding;
using kOS.Suffixed;

namespace kOS.Binding
{
    [Binding("ksp")]
    public class BindingConfig : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            shared.BindingMgr.AddGetter("CONFIG", cpu => Config.Instance);
        }
    }
}
