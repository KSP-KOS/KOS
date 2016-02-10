using kOS.Safe.Binding;
using kOS.Safe.Utilities;
using kOS.Suffixed;

namespace kOS.Binding
{
    [Binding("ksp")]
    public class BindingConfig : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            shared.BindingMgr.AddGetter("CONFIG", () => SafeHouse.Config);
            shared.BindingMgr.AddGetter("ADDONS", () => new AddonList(shared));
        }
    }
}
