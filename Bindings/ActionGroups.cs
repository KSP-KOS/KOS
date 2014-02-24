using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Utilities;
using kOS.Execution;

namespace kOS.Bindings
{
    [kOSBinding("ksp")]
    public class BindingActionGroups : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            _shared = shared;

            _shared.BindingMgr.AddSetter("SAS", delegate(CPU cpu, object val) { _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, (bool)val); });
            _shared.BindingMgr.AddSetter("GEAR", delegate(CPU cpu, object val) { _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Gear, (bool)val); });
            _shared.BindingMgr.AddSetter("LEGS", delegate(CPU cpu, object val) { VesselUtils.LandingLegsCtrl(_shared.Vessel, (bool)val); });
            _shared.BindingMgr.AddSetter("CHUTES", delegate(CPU cpu, object val) { VesselUtils.DeployParachutes(_shared.Vessel, (bool)val); });
            _shared.BindingMgr.AddSetter("LIGHTS", delegate(CPU cpu, object val) { _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Light, (bool)val); });
            _shared.BindingMgr.AddSetter("PANELS", delegate(CPU cpu, object val) { VesselUtils.SolarPanelCtrl(_shared.Vessel, (bool)val); });
            _shared.BindingMgr.AddSetter("BRAKES", delegate(CPU cpu, object val) { _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, (bool)val); });
            _shared.BindingMgr.AddSetter("RCS", delegate(CPU cpu, object val) { _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, (bool)val); });
            _shared.BindingMgr.AddSetter("ABORT", delegate(CPU cpu, object val) { _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Abort, (bool)val); });
            _shared.BindingMgr.AddSetter("AG1", delegate(CPU cpu, object val) { _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom01, (bool)val); });
            _shared.BindingMgr.AddSetter("AG2", delegate(CPU cpu, object val) { _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom02, (bool)val); });
            _shared.BindingMgr.AddSetter("AG3", delegate(CPU cpu, object val) { _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom03, (bool)val); });
            _shared.BindingMgr.AddSetter("AG4", delegate(CPU cpu, object val) { _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom04, (bool)val); });
            _shared.BindingMgr.AddSetter("AG5", delegate(CPU cpu, object val) { _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom05, (bool)val); });
            _shared.BindingMgr.AddSetter("AG6", delegate(CPU cpu, object val) { _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom06, (bool)val); });
            _shared.BindingMgr.AddSetter("AG7", delegate(CPU cpu, object val) { _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom07, (bool)val); });
            _shared.BindingMgr.AddSetter("AG8", delegate(CPU cpu, object val) { _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom08, (bool)val); });
            _shared.BindingMgr.AddSetter("AG9", delegate(CPU cpu, object val) { _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom09, (bool)val); });
            _shared.BindingMgr.AddSetter("AG10", delegate(CPU cpu, object val) { _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom10, (bool)val); });

            _shared.BindingMgr.AddGetter("SAS", delegate(CPU cpu) { return _shared.Vessel.ActionGroups[KSPActionGroup.SAS]; });
            _shared.BindingMgr.AddGetter("GEAR", delegate(CPU cpu) { return _shared.Vessel.ActionGroups[KSPActionGroup.Gear]; });
            _shared.BindingMgr.AddGetter("LEGS", delegate(CPU cpu) { return VesselUtils.GetLandingLegStatus(_shared.Vessel); });
            _shared.BindingMgr.AddGetter("CHUTES", delegate(CPU cpu) { return VesselUtils.GetChuteStatus(_shared.Vessel); });
            _shared.BindingMgr.AddGetter("LIGHTS", delegate(CPU cpu) { return _shared.Vessel.ActionGroups[KSPActionGroup.Light]; });
            _shared.BindingMgr.AddGetter("PANELS", delegate(CPU cpu) { return VesselUtils.GetSolarPanelStatus(_shared.Vessel); });
            _shared.BindingMgr.AddGetter("BRAKES", delegate(CPU cpu) { return _shared.Vessel.ActionGroups[KSPActionGroup.Brakes]; });
            _shared.BindingMgr.AddGetter("RCS", delegate(CPU cpu) { return _shared.Vessel.ActionGroups[KSPActionGroup.RCS]; });
            _shared.BindingMgr.AddGetter("ABORT", delegate(CPU cpu) { return _shared.Vessel.ActionGroups[KSPActionGroup.Abort]; });
            _shared.BindingMgr.AddGetter("AG1", delegate(CPU cpu) { return _shared.Vessel.ActionGroups[KSPActionGroup.Custom01]; });
            _shared.BindingMgr.AddGetter("AG2", delegate(CPU cpu) { return _shared.Vessel.ActionGroups[KSPActionGroup.Custom02]; });
            _shared.BindingMgr.AddGetter("AG3", delegate(CPU cpu) { return _shared.Vessel.ActionGroups[KSPActionGroup.Custom03]; });
            _shared.BindingMgr.AddGetter("AG4", delegate(CPU cpu) { return _shared.Vessel.ActionGroups[KSPActionGroup.Custom04]; });
            _shared.BindingMgr.AddGetter("AG5", delegate(CPU cpu) { return _shared.Vessel.ActionGroups[KSPActionGroup.Custom05]; });
            _shared.BindingMgr.AddGetter("AG6", delegate(CPU cpu) { return _shared.Vessel.ActionGroups[KSPActionGroup.Custom06]; });
            _shared.BindingMgr.AddGetter("AG7", delegate(CPU cpu) { return _shared.Vessel.ActionGroups[KSPActionGroup.Custom07]; });
            _shared.BindingMgr.AddGetter("AG8", delegate(CPU cpu) { return _shared.Vessel.ActionGroups[KSPActionGroup.Custom08]; });
            _shared.BindingMgr.AddGetter("AG9", delegate(CPU cpu) { return _shared.Vessel.ActionGroups[KSPActionGroup.Custom09]; });
            _shared.BindingMgr.AddGetter("AG10", delegate(CPU cpu) { return _shared.Vessel.ActionGroups[KSPActionGroup.Custom10]; });
        }
    }
}
