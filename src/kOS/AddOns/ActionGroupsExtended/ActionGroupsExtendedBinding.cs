using kOS.Safe.Binding;

namespace kOS.AddOns.ActionGroupsExtended
{
    [Binding("ksp")]
    public class ActionGroupsExtendedBinding : Binding.Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            if (!ActionGroupsExtendedAPI.Instance.Installed()) return;

            for (int outerIndex = 11; outerIndex <= 250; outerIndex++)
            {
                int i = outerIndex;
                shared.BindingMgr.AddSetter("AG" + i, val => ActionGroupsExtendedAPI.Instance.ActivateGroup(shared.Vessel.rootPart.flightID, i, (bool) val));
                shared.BindingMgr.AddGetter("AG" + i, () => ActionGroupsExtendedAPI.Instance.GetGroupState(shared.Vessel.rootPart.flightID, i));
            }
        }
    }
}
