using kOS.Utilities;

namespace kOS.Binding
{
    [kOSBinding("ksp")]
    public class ActionGroups : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            Shared = shared;

            Shared.BindingMgr.AddSetter("SAS", (cpu, val) => Shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, (bool) val));
            Shared.BindingMgr.AddSetter("GEAR", (cpu, val) => Shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Gear, (bool) val));
            Shared.BindingMgr.AddSetter("LEGS", (cpu, val) => VesselUtils.LandingLegsCtrl(Shared.Vessel, (bool) val));
            Shared.BindingMgr.AddSetter("CHUTES", (cpu, val) => VesselUtils.DeployParachutes(Shared.Vessel, (bool) val));
            Shared.BindingMgr.AddSetter("LIGHTS", (cpu, val) => Shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Light, (bool) val));
            Shared.BindingMgr.AddSetter("PANELS", (cpu, val) => VesselUtils.SolarPanelCtrl(Shared.Vessel, (bool) val));
            Shared.BindingMgr.AddSetter("BRAKES", (cpu, val) => Shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, (bool) val));
            Shared.BindingMgr.AddSetter("RCS", (cpu, val) => Shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, (bool) val));
            Shared.BindingMgr.AddSetter("ABORT", (cpu, val) => Shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Abort, (bool) val));
            Shared.BindingMgr.AddSetter("AG1", (cpu, val) => Shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom01, (bool) val));
            Shared.BindingMgr.AddSetter("AG2", (cpu, val) => Shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom02, (bool) val));
            Shared.BindingMgr.AddSetter("AG3", (cpu, val) => Shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom03, (bool) val));
            Shared.BindingMgr.AddSetter("AG4", (cpu, val) => Shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom04, (bool) val));
            Shared.BindingMgr.AddSetter("AG5", (cpu, val) => Shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom05, (bool) val));
            Shared.BindingMgr.AddSetter("AG6", (cpu, val) => Shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom06, (bool) val));
            Shared.BindingMgr.AddSetter("AG7", (cpu, val) => Shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom07, (bool) val));
            Shared.BindingMgr.AddSetter("AG8", (cpu, val) => Shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom08, (bool) val));
            Shared.BindingMgr.AddSetter("AG9", (cpu, val) => Shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom09, (bool) val));
            Shared.BindingMgr.AddSetter("AG10", (cpu, val) => Shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom10, (bool) val));

            Shared.BindingMgr.AddGetter("SAS", cpu => Shared.Vessel.ActionGroups[KSPActionGroup.SAS]);
            Shared.BindingMgr.AddGetter("GEAR", cpu => Shared.Vessel.ActionGroups[KSPActionGroup.Gear]);
            Shared.BindingMgr.AddGetter("LEGS", cpu => VesselUtils.GetLandingLegStatus(Shared.Vessel));
            Shared.BindingMgr.AddGetter("CHUTES", cpu => VesselUtils.GetChuteStatus(Shared.Vessel));
            Shared.BindingMgr.AddGetter("LIGHTS", cpu => Shared.Vessel.ActionGroups[KSPActionGroup.Light]);
            Shared.BindingMgr.AddGetter("PANELS", cpu => VesselUtils.GetSolarPanelStatus(Shared.Vessel));
            Shared.BindingMgr.AddGetter("BRAKES", cpu => Shared.Vessel.ActionGroups[KSPActionGroup.Brakes]);
            Shared.BindingMgr.AddGetter("RCS", cpu => Shared.Vessel.ActionGroups[KSPActionGroup.RCS]);
            Shared.BindingMgr.AddGetter("ABORT", cpu => Shared.Vessel.ActionGroups[KSPActionGroup.Abort]);
            Shared.BindingMgr.AddGetter("AG1", cpu => Shared.Vessel.ActionGroups[KSPActionGroup.Custom01]);
            Shared.BindingMgr.AddGetter("AG2", cpu => Shared.Vessel.ActionGroups[KSPActionGroup.Custom02]);
            Shared.BindingMgr.AddGetter("AG3", cpu => Shared.Vessel.ActionGroups[KSPActionGroup.Custom03]);
            Shared.BindingMgr.AddGetter("AG4", cpu => Shared.Vessel.ActionGroups[KSPActionGroup.Custom04]);
            Shared.BindingMgr.AddGetter("AG5", cpu => Shared.Vessel.ActionGroups[KSPActionGroup.Custom05]);
            Shared.BindingMgr.AddGetter("AG6", cpu => Shared.Vessel.ActionGroups[KSPActionGroup.Custom06]);
            Shared.BindingMgr.AddGetter("AG7", cpu => Shared.Vessel.ActionGroups[KSPActionGroup.Custom07]);
            Shared.BindingMgr.AddGetter("AG8", cpu => Shared.Vessel.ActionGroups[KSPActionGroup.Custom08]);
            Shared.BindingMgr.AddGetter("AG9", cpu => Shared.Vessel.ActionGroups[KSPActionGroup.Custom09]);
            Shared.BindingMgr.AddGetter("AG10", cpu => Shared.Vessel.ActionGroups[KSPActionGroup.Custom10]);
            
        }
    }
}