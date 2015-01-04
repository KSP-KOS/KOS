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

            if (AGExtInstalled())
            {
                for (int i2 = 11; i2 <= 250; i2++)
                {
                    int i = i2;
                    shared.BindingMgr.AddSetter("AG"+i.ToString(), val => AGX2VslActivateGroup(shared.Vessel.rootPart.flightID, i, (bool)val));
                    shared.BindingMgr.AddGetter("AG"+i.ToString(), () => AGX2VslGroupState(shared.Vessel.rootPart.flightID, i));
                }
            }

        }

        public static bool AGExtInstalled() //is AGX installed?
        {
            try //try-catch is required as the below code returns a NullRef if AGX is not present.
            {
                System.Type calledType = System.Type.GetType("ActionGroupsExtended.AGExtExternal, AGExt");
                return (bool)calledType.InvokeMember("AGXInstalled", System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null, null, null);
            }
            catch
            {
                return false;
            }
        }

        public static bool AGX2VslActivateGroup(uint FlightID, int group, bool forceDir) //activate/deactivate an action group
        {
            
            System.Type calledType = System.Type.GetType("ActionGroupsExtended.AGExtExternal, AGExt");
            bool GroupAct = (bool)calledType.InvokeMember("AGX2VslActivateGroup", System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null, null, new System.Object[] { FlightID, group, forceDir });
            return GroupAct;
        }

        public static bool AGX2VslGroupState(uint FlightID, int group)
        {
            System.Type calledType = System.Type.GetType("ActionGroupsExtended.AGExtExternal, AGExt");
            bool GroupAct = (bool)calledType.InvokeMember("AGX2VslGroupState", System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null, null, new System.Object[] { FlightID, group });
            return GroupAct;
        }
    }
}