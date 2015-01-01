using System;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Encapsulation;

namespace kOS.Suffixed
{
    public class Career : Structure
    {
        static Career()
        {
            AddGlobalSuffix<Career>("CANTRACKOBJECTS", new StaticSuffix<bool>(CanTrackObjects,
                                                                              "Can the Tracking Center track small space objects (asteroids)?"));
            AddGlobalSuffix<Career>("PATCHLIMIT", new StaticSuffix<int>(PatchLimit,
                                                                        "The Tracking Center's orbit patch prediction limit (an integer)"));
            AddGlobalSuffix<Career>("CANMAKENODES", new StaticSuffix<bool>(CanMakeNodes,
                                                                           "Can the Mission Control support maneuver nodes yet?"));
            AddGlobalSuffix<Career>("CANDOACTIONS", new StaticSuffix<bool>(CanDoActions,
                                                                           "Can either the VAB or SPH allow custom action groups? " +
                                                                           "If either one allows it, then you are allowed to call " +
                                                                           "the :DOACTION suffix of a PartModule." ));
        }

        /// <summary>
        /// Detect whether or not you can track small space objects like asteroids yet.
        /// </summary>
        /// <param name="reason">returns a string describing what would need upgrading to change the answer.</param>
        /// <returns>true if you can.  false if you cannot.</returns>
        public static bool CanTrackObjects(out string reason)
        {
            reason = "tracking station building";
            float buildingLevel = ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.TrackingStation);
            return GameVariables.Instance.UnlockedSpaceObjectDiscovery(buildingLevel);
            
            // TODO: This isn't really being used anywhere yet because it's supposed to apply to the ability to track
            // and name asteroids.  So far I don't know yet where that would be used in kOS.  I guess it might be
            // checked if you try to make a Vessel() wrapper around an asteroid?  (are Asteroids Vessels?  I think they
            // are?)  The UI will already prevent you from doing a SET TARGET TO <asteroid> if your tech isn't right for it.
        }

        /// <summary>
        /// Same as CanTrackObjects, but without the 'reason' parameter.  (This is a separate method
        /// only because you can't default out parameters like 'out string reason' to make them optional.)
        /// </summary>
        /// <returns>true if you can. false if you cannot.</returns>
        public static bool CanTrackObjects()
        {
            string dummy;
            return CanTrackObjects(out dummy);
        }

        /// <summary>
        /// Detect how good the tracking center is at seeing future SOI transfers (encounters, escapes, etc).
        /// </summary>
        /// <param name="reason">returns a string describing what would need upgrading to change the answer.</param>
        /// <returns>Maximum number of patches that can be predicted ahead (not including the current one).</returns>
        public static int PatchLimit(out string reason)
        {
            reason = "tracking station building";
            float buildingLevel = ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.TrackingStation);
            return GameVariables.Instance.GetPatchesAheadLimit(buildingLevel);
        }

        /// <summary>
        /// Same as PatchLImit, but without the 'reason' parameter.  (This is a separate method
        /// only because you can't default out parameters like 'out string reason' to make them optional.)
        /// </summary>
        /// <returns>max number of patches ahead (not including the current one).</returns>
        public static int PatchLimit()
        {
            string dummy;
            return PatchLimit(out dummy);
        }

        /// <summary>
        /// Detect whether mission control is capable of maneuver node creation yet.
        /// </summary>
        /// <param name="reason">returns a string describing what would need upgrading to change the answer.</param>
        /// <returns>true if it can. false if it cannot.</returns>
        public static bool CanMakeNodes(out string reason)
        {
            // This one is a weird check.  It requires TWO building conditions, as far as I can tell:
            reason = "mission control building";
            float buildingLevel = ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.MissionControl);
            if (! GameVariables.Instance.UnlockedFlightPlanning(buildingLevel))
                return false;
            return (PatchLimit(out reason) > 0); // if the tracking center isn't good enough then that ALSO prevents maneuver nodes.
        }

        /// <summary>
        /// Same as CanMakeNodes, but without the 'reason' parameter.  (This is a separate method
        /// only because you can't default out parameters like 'out string reason' to make them optional.)
        /// </summary>
        /// <returns>true if you can. false if you cannot.</returns>
        public static bool CanMakeNodes()
        {
            string dummy;
            return CanMakeNodes(out dummy);
        }

        /// <summary>
        /// Detect whether we'll allow arbitrary use of :DOACTION from partmodules, which
        /// depends on whether at least one of the editors (VAB,SPH) allows custom action groups:
        /// </summary>
        /// <param name="reason">returns a string describing what would need upgrading to change the answer.</param>
        /// <returns>true if it can. false if it cannot.</returns>
        public static bool CanDoActions(out string reason)
        {
            reason = "vehicle assembly building or space plane hangar";
            float buildingLevel = Math.Max(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.VehicleAssemblyBuilding),
                                            ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.SpaceplaneHangar));
            return GameVariables.Instance.UnlockedActionGroupsCustom(buildingLevel);
        }

        /// <summary>
        /// Same as CanDoActions, but without the 'reason' parameter.  (This is a separate method
        /// only because you can't default out parameters like 'out string reason' to make them optional.)
        /// </summary>
        /// <returns>true if you can. false if you cannot.</returns>
        public static bool CanDoActions()
        {
            string dummy;
            return CanDoActions(out dummy);
        }
        
        /// <summary>
        /// Detect whether we'll allow you to add a NameTag to a part during the editor.
        /// You can always add one post-editor during flight, but during editing it will
        /// depend on building level.<br/>
        /// NOTE: This method does NOT have an associated suffix because it is meant to be called
        /// from inside the editor, when a script won't be running.
        /// </summary>
        /// <param name="whichEditor">Pass in whether you are checking from the VAB or SPH.</param>
        /// <param name="reason">returns a string describing what would need upgrading to change the answer.</param>
        /// <returns>true if it can. false if it cannot.</returns>
        public static bool CanTagInEditor(EditorFacility whichEditor, out string reason)
        {
            float buildingLevel;
            switch (whichEditor)
            {
                case EditorFacility.VAB:
                    reason = "vehicle assembly building";
                    buildingLevel = ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.VehicleAssemblyBuilding);
                    break;
                case EditorFacility.SPH:
                    reason = "space plane hangar";
                    buildingLevel = ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.SpaceplaneHangar);
                    break;
                default:
                    reason = "unknown editor building";
                    return false; // not even sure how you could get here.
            }
            // We'll attach it to the same point where the game starts unlocking basic action groups:
            return GameVariables.Instance.UnlockedActionGroupsStock(buildingLevel);
        }

        /// <summary>
        /// Same as CanTagInEditor, but without the 'reason' parameter.  (This is a separate method
        /// only because you can't default out parameters like 'out string reason' to make them optional.)
        /// </summary>
        /// <returns>true if you can. false if you cannot.</returns>
        public static bool CanTagInEditor(EditorFacility whichEditor)
        {
            string dummy;
            return CanTagInEditor(whichEditor, out dummy);
        }

        public override string ToString()
        {
            return string.Format("{0} Career", base.ToString());
        }
    }

}