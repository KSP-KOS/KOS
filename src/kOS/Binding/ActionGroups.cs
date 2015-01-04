using kOS.Safe.Binding;
using kOS.Utilities;

namespace kOS.Binding
{
    [Binding("ksp")]
    public class ActionGroups : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            shared.BindingMgr.AddSetter("SAS", val => shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, (bool) val));
            shared.BindingMgr.AddSetter("GEAR", val => shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Gear, (bool) val));
            shared.BindingMgr.AddSetter("LEGS", val => VesselUtils.LandingLegsCtrl(shared.Vessel, (bool) val));
            shared.BindingMgr.AddSetter("CHUTES", val => VesselUtils.DeployParachutes(shared.Vessel, (bool) val));
            shared.BindingMgr.AddSetter("LIGHTS", val => shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Light, (bool) val));
            shared.BindingMgr.AddSetter("PANELS", val => VesselUtils.SolarPanelCtrl(shared.Vessel, (bool) val));
            shared.BindingMgr.AddSetter("BRAKES", val => shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, (bool) val));
            shared.BindingMgr.AddSetter("RCS", val => shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, (bool) val));
            shared.BindingMgr.AddSetter("ABORT", val => shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Abort, (bool) val));
            shared.BindingMgr.AddSetter("AG1", val => shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom01, (bool) val));
            shared.BindingMgr.AddSetter("AG2", val => shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom02, (bool) val));
            shared.BindingMgr.AddSetter("AG3", val => shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom03, (bool) val));
            shared.BindingMgr.AddSetter("AG4", val => shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom04, (bool) val));
            shared.BindingMgr.AddSetter("AG5", val => shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom05, (bool) val));
            shared.BindingMgr.AddSetter("AG6", val => shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom06, (bool) val));
            shared.BindingMgr.AddSetter("AG7", val => shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom07, (bool) val));
            shared.BindingMgr.AddSetter("AG8", val => shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom08, (bool) val));
            shared.BindingMgr.AddSetter("AG9", val => shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom09, (bool) val));
            shared.BindingMgr.AddSetter("AG10", val => shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom10, (bool) val));

            shared.BindingMgr.AddGetter("SAS", () => shared.Vessel.ActionGroups[KSPActionGroup.SAS]);
            shared.BindingMgr.AddGetter("GEAR", () => shared.Vessel.ActionGroups[KSPActionGroup.Gear]);
            shared.BindingMgr.AddGetter("LEGS", () => VesselUtils.GetLandingLegStatus(shared.Vessel));
            shared.BindingMgr.AddGetter("CHUTES", () => VesselUtils.GetChuteStatus(shared.Vessel));
            shared.BindingMgr.AddGetter("LIGHTS", () => shared.Vessel.ActionGroups[KSPActionGroup.Light]);
            shared.BindingMgr.AddGetter("PANELS", () => VesselUtils.GetSolarPanelStatus(shared.Vessel));
            shared.BindingMgr.AddGetter("BRAKES", () => shared.Vessel.ActionGroups[KSPActionGroup.Brakes]);
            shared.BindingMgr.AddGetter("RCS", () => shared.Vessel.ActionGroups[KSPActionGroup.RCS]);
            shared.BindingMgr.AddGetter("ABORT", () => shared.Vessel.ActionGroups[KSPActionGroup.Abort]);
            shared.BindingMgr.AddGetter("AG1", () => shared.Vessel.ActionGroups[KSPActionGroup.Custom01]);
            shared.BindingMgr.AddGetter("AG2", () => shared.Vessel.ActionGroups[KSPActionGroup.Custom02]);
            shared.BindingMgr.AddGetter("AG3", () => shared.Vessel.ActionGroups[KSPActionGroup.Custom03]);
            shared.BindingMgr.AddGetter("AG4", () => shared.Vessel.ActionGroups[KSPActionGroup.Custom04]);
            shared.BindingMgr.AddGetter("AG5", () => shared.Vessel.ActionGroups[KSPActionGroup.Custom05]);
            shared.BindingMgr.AddGetter("AG6", () => shared.Vessel.ActionGroups[KSPActionGroup.Custom06]);
            shared.BindingMgr.AddGetter("AG7", () => shared.Vessel.ActionGroups[KSPActionGroup.Custom07]);
            shared.BindingMgr.AddGetter("AG8", () => shared.Vessel.ActionGroups[KSPActionGroup.Custom08]);
            shared.BindingMgr.AddGetter("AG9", () => shared.Vessel.ActionGroups[KSPActionGroup.Custom09]);
            shared.BindingMgr.AddGetter("AG10", () => shared.Vessel.ActionGroups[KSPActionGroup.Custom10]);
        }

    }
}