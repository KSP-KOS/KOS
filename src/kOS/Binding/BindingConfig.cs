using kOS.Suffixed;

namespace kOS.Binding
{
    [kOSBinding("ksp")]
    public class BindingConfig : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            Shared = shared;
            Shared.BindingMgr.AddGetter("CONFIG", cpu => Config.Instance);
        }
    }
}
