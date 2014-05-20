using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Execution;
using kOS.Utilities;

namespace kOS.Binding
{
    [kOSBinding("ksp")]
    public class ActionGroups : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            _shared = shared;

            _shared.BindingMgr.AddSetter("SAS", (cpu, val) => _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, (bool) val));
            _shared.BindingMgr.AddSetter("GEAR", (cpu, val) => _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Gear, (bool) val));
            _shared.BindingMgr.AddSetter("LEGS", (cpu, val) => VesselUtils.LandingLegsCtrl(_shared.Vessel, (bool) val));
            _shared.BindingMgr.AddSetter("CHUTES", (cpu, val) => VesselUtils.DeployParachutes(_shared.Vessel, (bool) val));
            _shared.BindingMgr.AddSetter("LIGHTS", (cpu, val) => _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Light, (bool) val));
            _shared.BindingMgr.AddSetter("PANELS", (cpu, val) => VesselUtils.SolarPanelCtrl(_shared.Vessel, (bool) val));
            _shared.BindingMgr.AddSetter("BRAKES", (cpu, val) => _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, (bool) val));
            _shared.BindingMgr.AddSetter("RCS", (cpu, val) => _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, (bool) val));
            _shared.BindingMgr.AddSetter("ABORT", (cpu, val) => _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Abort, (bool) val));
            _shared.BindingMgr.AddSetter("AG1", (cpu, val) => _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom01, (bool) val));
            _shared.BindingMgr.AddSetter("AG2", (cpu, val) => _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom02, (bool) val));
            _shared.BindingMgr.AddSetter("AG3", (cpu, val) => _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom03, (bool) val));
            _shared.BindingMgr.AddSetter("AG4", (cpu, val) => _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom04, (bool) val));
            _shared.BindingMgr.AddSetter("AG5", (cpu, val) => _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom05, (bool) val));
            _shared.BindingMgr.AddSetter("AG6", (cpu, val) => _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom06, (bool) val));
            _shared.BindingMgr.AddSetter("AG7", (cpu, val) => _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom07, (bool) val));
            _shared.BindingMgr.AddSetter("AG8", (cpu, val) => _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom08, (bool) val));
            _shared.BindingMgr.AddSetter("AG9", (cpu, val) => _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom09, (bool) val));
            _shared.BindingMgr.AddSetter("AG10", (cpu, val) => _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom10, (bool) val));

            _shared.BindingMgr.AddGetter("SAS", cpu => _shared.Vessel.ActionGroups[KSPActionGroup.SAS]);
            _shared.BindingMgr.AddGetter("GEAR", cpu => _shared.Vessel.ActionGroups[KSPActionGroup.Gear]);
            _shared.BindingMgr.AddGetter("LEGS", cpu => VesselUtils.GetLandingLegStatus(_shared.Vessel));
            _shared.BindingMgr.AddGetter("CHUTES", cpu => VesselUtils.GetChuteStatus(_shared.Vessel));
            _shared.BindingMgr.AddGetter("LIGHTS", cpu => _shared.Vessel.ActionGroups[KSPActionGroup.Light]);
            _shared.BindingMgr.AddGetter("PANELS", cpu => VesselUtils.GetSolarPanelStatus(_shared.Vessel));
            _shared.BindingMgr.AddGetter("BRAKES", cpu => _shared.Vessel.ActionGroups[KSPActionGroup.Brakes]);
            _shared.BindingMgr.AddGetter("RCS", cpu => _shared.Vessel.ActionGroups[KSPActionGroup.RCS]);
            _shared.BindingMgr.AddGetter("ABORT", cpu => _shared.Vessel.ActionGroups[KSPActionGroup.Abort]);
            _shared.BindingMgr.AddGetter("AG1", cpu => _shared.Vessel.ActionGroups[KSPActionGroup.Custom01]);
            _shared.BindingMgr.AddGetter("AG2", cpu => _shared.Vessel.ActionGroups[KSPActionGroup.Custom02]);
            _shared.BindingMgr.AddGetter("AG3", cpu => _shared.Vessel.ActionGroups[KSPActionGroup.Custom03]);
            _shared.BindingMgr.AddGetter("AG4", cpu => _shared.Vessel.ActionGroups[KSPActionGroup.Custom04]);
            _shared.BindingMgr.AddGetter("AG5", cpu => _shared.Vessel.ActionGroups[KSPActionGroup.Custom05]);
            _shared.BindingMgr.AddGetter("AG6", cpu => _shared.Vessel.ActionGroups[KSPActionGroup.Custom06]);
            _shared.BindingMgr.AddGetter("AG7", cpu => _shared.Vessel.ActionGroups[KSPActionGroup.Custom07]);
            _shared.BindingMgr.AddGetter("AG8", cpu => _shared.Vessel.ActionGroups[KSPActionGroup.Custom08]);
            _shared.BindingMgr.AddGetter("AG9", cpu => _shared.Vessel.ActionGroups[KSPActionGroup.Custom09]);
            _shared.BindingMgr.AddGetter("AG10", cpu => _shared.Vessel.ActionGroups[KSPActionGroup.Custom10]);
            
        }
    }
}