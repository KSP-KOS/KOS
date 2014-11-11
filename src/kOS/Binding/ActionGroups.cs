using kOS.Safe.Binding;
using kOS.Utilities;

namespace kOS.Binding
{
    [Binding("ksp")]
    public class ActionGroups : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            shared.BindingMgr.AddSetter("SAS", (cpu, val) => shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, (bool) val));
            shared.BindingMgr.AddSetter("GEAR", (cpu, val) => shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Gear, (bool) val));
            shared.BindingMgr.AddSetter("LEGS", (cpu, val) => VesselUtils.LandingLegsCtrl(shared.Vessel, (bool) val));
            shared.BindingMgr.AddSetter("CHUTES", (cpu, val) => VesselUtils.DeployParachutes(shared.Vessel, (bool) val));
            shared.BindingMgr.AddSetter("LIGHTS", (cpu, val) => shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Light, (bool) val));
            shared.BindingMgr.AddSetter("PANELS", (cpu, val) => VesselUtils.SolarPanelCtrl(shared.Vessel, (bool) val));
            shared.BindingMgr.AddSetter("BRAKES", (cpu, val) => shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, (bool) val));
            shared.BindingMgr.AddSetter("RCS", (cpu, val) => shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, (bool) val));
            shared.BindingMgr.AddSetter("ABORT", (cpu, val) => shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Abort, (bool) val));
            shared.BindingMgr.AddSetter("AG1", (cpu, val) => shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom01, (bool) val));
            shared.BindingMgr.AddSetter("AG2", (cpu, val) => shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom02, (bool) val));
            shared.BindingMgr.AddSetter("AG3", (cpu, val) => shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom03, (bool) val));
            shared.BindingMgr.AddSetter("AG4", (cpu, val) => shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom04, (bool) val));
            shared.BindingMgr.AddSetter("AG5", (cpu, val) => shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom05, (bool) val));
            shared.BindingMgr.AddSetter("AG6", (cpu, val) => shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom06, (bool) val));
            shared.BindingMgr.AddSetter("AG7", (cpu, val) => shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom07, (bool) val));
            shared.BindingMgr.AddSetter("AG8", (cpu, val) => shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom08, (bool) val));
            shared.BindingMgr.AddSetter("AG9", (cpu, val) => shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom09, (bool) val));
            shared.BindingMgr.AddSetter("AG10", (cpu, val) => shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom10, (bool) val));

            shared.BindingMgr.AddGetter("SAS", cpu => shared.Vessel.ActionGroups[KSPActionGroup.SAS]);
            shared.BindingMgr.AddGetter("GEAR", cpu => shared.Vessel.ActionGroups[KSPActionGroup.Gear]);
            shared.BindingMgr.AddGetter("LEGS", cpu => VesselUtils.GetLandingLegStatus(shared.Vessel));
            shared.BindingMgr.AddGetter("CHUTES", cpu => VesselUtils.GetChuteStatus(shared.Vessel));
            shared.BindingMgr.AddGetter("LIGHTS", cpu => shared.Vessel.ActionGroups[KSPActionGroup.Light]);
            shared.BindingMgr.AddGetter("PANELS", cpu => VesselUtils.GetSolarPanelStatus(shared.Vessel));
            shared.BindingMgr.AddGetter("BRAKES", cpu => shared.Vessel.ActionGroups[KSPActionGroup.Brakes]);
            shared.BindingMgr.AddGetter("RCS", cpu => shared.Vessel.ActionGroups[KSPActionGroup.RCS]);
            shared.BindingMgr.AddGetter("ABORT", cpu => shared.Vessel.ActionGroups[KSPActionGroup.Abort]);
            shared.BindingMgr.AddGetter("AG1", cpu => shared.Vessel.ActionGroups[KSPActionGroup.Custom01]);
            shared.BindingMgr.AddGetter("AG2", cpu => shared.Vessel.ActionGroups[KSPActionGroup.Custom02]);
            shared.BindingMgr.AddGetter("AG3", cpu => shared.Vessel.ActionGroups[KSPActionGroup.Custom03]);
            shared.BindingMgr.AddGetter("AG4", cpu => shared.Vessel.ActionGroups[KSPActionGroup.Custom04]);
            shared.BindingMgr.AddGetter("AG5", cpu => shared.Vessel.ActionGroups[KSPActionGroup.Custom05]);
            shared.BindingMgr.AddGetter("AG6", cpu => shared.Vessel.ActionGroups[KSPActionGroup.Custom06]);
            shared.BindingMgr.AddGetter("AG7", cpu => shared.Vessel.ActionGroups[KSPActionGroup.Custom07]);
            shared.BindingMgr.AddGetter("AG8", cpu => shared.Vessel.ActionGroups[KSPActionGroup.Custom08]);
            shared.BindingMgr.AddGetter("AG9", cpu => shared.Vessel.ActionGroups[KSPActionGroup.Custom09]);
            shared.BindingMgr.AddGetter("AG10", cpu => shared.Vessel.ActionGroups[KSPActionGroup.Custom10]);
            
        }
    }
}