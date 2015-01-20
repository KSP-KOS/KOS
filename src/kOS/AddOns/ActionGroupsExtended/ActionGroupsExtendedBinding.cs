using kOS.Safe.Binding;

namespace kOS.AddOns.ActionGroupsExtended
{
    [Binding("ksp")]
    public class ActionGroupsExtendedBinding : Binding.Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            if (!ActionGroupsExtendedAPI.Instance.Installed()) return;

            var vessel = shared.Vessel;
            var api = ActionGroupsExtendedAPI.Instance;

            for (int outerIndex = 11; outerIndex <= 250; outerIndex++)
            {
                int i = outerIndex;
                shared.BindingMgr.AddSetter("AG" + i, val => api.ActivateGroup(vessel, i, (bool) val));
                shared.BindingMgr.AddGetter("AG" + i, () => api.GetGroupState(vessel, i));
            }
        }
    }
}
