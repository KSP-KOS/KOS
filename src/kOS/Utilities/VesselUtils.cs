using kOS.Safe.Encapsulation;
using kOS.Suffixed;
using kOS.Suffixed.Part;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace kOS.Utilities
{
    public static class VesselUtils
    {
        public static bool HasCrewControl(this Vessel vessel)
        {
            return vessel.parts.Any(p => p.isControlSource && (p.protoModuleCrew.Any()));
        }

        public static List<Part> GetListOfActivatedEngines(Vessel vessel)
        {
            var retList = new List<Part>();

            foreach (var part in vessel.Parts)
            {
                foreach (PartModule module in part.Modules)
                {
                    var engineModule = module as ModuleEngines;
                    var engineModuleFx = module as ModuleEnginesFX;

                    if (engineModuleFx != null)
                    {
                        if (engineModuleFx.getIgnitionState)
                        {
                            retList.Add(part);
                        }
                    }
                    else if (engineModule != null)
                    {
                        if (engineModule.getIgnitionState)
                        {
                            retList.Add(part);
                        }
                    }
                }
            }

            return retList;
        }

        public static bool TryGetResource(Vessel vessel, string resourceName, out double total)
        {
            var resourceIsFound = false;
            total = 0;
            PartResourceDefinition resourceDefinition =
                PartResourceLibrary.Instance.resourceDefinitions.FirstOrDefault(
                    rd => rd.name.Equals(resourceName, StringComparison.CurrentCultureIgnoreCase));
            // Ensure the built-in resource types never produce an error, even if the particular vessel is incapable of carrying them
            if (resourceDefinition != null)
                resourceIsFound = true;
            resourceName = resourceName.ToUpper();
            foreach (var part in vessel.parts)
            {
                foreach (PartResource resource in part.Resources)
                {
                    if (resource.resourceName.ToUpper() != resourceName) continue;
                    resourceIsFound = true;
                    total += resource.amount;
                }
            }

            return resourceIsFound;
        }

        public static ListValue PartList(this IShipconstruct vessel, string partType, SharedObjects sharedObj)
        {
            var list = new ListValue();
            var partList = vessel.Parts.ToList();

            switch (partType.ToUpper())
            {
                case "RESOURCES":
                    list = AggregateResourceValue.PartsToList(partList, sharedObj);
                    break;

                case "PARTS":
                    list = PartValueFactory.Construct(partList, sharedObj);
                    break;

                case "ENGINES":
                    list = EngineValue.PartsToList(partList, sharedObj);
                    break;

                case "SENSORS":
                    list = SensorValue.PartsToList(partList, sharedObj);
                    break;

                case "ELEMENTS":
                    list = ElementValue.PartsToList(partList, sharedObj);
                    break;

                case "DOCKINGPORTS":
                    list = DockingPortValue.PartsToList(partList, sharedObj);
                    break;
            }
            return list;
        }

        public static double GetMaxThrust(Vessel vessel, double atmPressure = -1.0)
        {
            var thrust = 0.0;

            foreach (var p in vessel.parts)
            {
                foreach (PartModule pm in p.Modules)
                {
                    if (!pm.isEnabled) continue;
                    if (pm is ModuleEngines)
                    {
                        thrust += ModuleEngineAdapter.GetEngineThrust((ModuleEngines)pm, atmPressure: atmPressure);
                    }
                }
            }

            return thrust;
        }

        private static Vessel TryGetVesselByName(string name, Vessel origin)
        {
            return FlightGlobals.Vessels.FirstOrDefault(v => v != origin && v.vesselName.ToUpper() == name.ToUpper());
        }

        public static CelestialBody GetBodyByName(string name)
        {
            return FlightGlobals.fetch.bodies.FirstOrDefault(body => name.ToUpper() == body.name.ToUpper());
        }

        public static Vessel GetVesselByName(string name, Vessel origin)
        {
            var vessel = TryGetVesselByName(name, origin);

            if (vessel == null)
            {
                throw new Exception("Vessel '" + name + "' not found");
            }
            return vessel;
        }

        public static void SetTarget(IKOSTargetable val)
        {
            if (val.Target != null)
            {
                SetTarget(val.Target);
            }
            else
            {
                throw new Exception("Error on targeting " + val);
            }
        }

        public static void SetTarget(ITargetable val)
        {
            FlightGlobals.fetch.SetVesselTarget(val);
        }

        public static float AngleDelta(float a, float b)
        {
            var delta = b - a;

            return (float)Utils.DegreeFix(delta, -180.0);
        }

        public static float GetHeading(Vessel vessel)
        {
            var up = vessel.upAxis;
            var north = GetNorthVector(vessel);
            var headingQ =
                Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(vessel.GetTransform().rotation) *
                                   Quaternion.LookRotation(north, up));

            return headingQ.eulerAngles.y;
        }

        public static float GetVelocityHeading(Vessel vessel)
        {
            var up = vessel.upAxis;
            var north = GetNorthVector(vessel);
            var headingQ =
                Quaternion.Inverse(Quaternion.Inverse(Quaternion.LookRotation(vessel.srf_velocity, up)) *
                                   Quaternion.LookRotation(north, up));

            return headingQ.eulerAngles.y;
        }

        public static float GetTargetBearing(Vessel vessel, Vessel target)
        {
            return AngleDelta(GetHeading(vessel), GetTargetHeading(vessel, target));
        }

        public static float GetTargetHeading(Vessel vessel, Vessel target)
        {
            var up = vessel.upAxis;
            var north = GetNorthVector(vessel);
            var vector =
                Vector3d.Exclude(vessel.upAxis, target.findWorldCenterOfMass() - vessel.findWorldCenterOfMass()).normalized;
            var headingQ =
                Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(Quaternion.LookRotation(vector, up)) *
                                   Quaternion.LookRotation(north, up));

            return headingQ.eulerAngles.y;
        }

        public static Vector3d GetNorthVector(Vessel vessel)
        {
            return Vector3d.Exclude(vessel.upAxis, vessel.mainBody.transform.up);
        }

        public static object TryGetEncounter(Vessel vessel, SharedObjects sharedObj)
        {
            // If not the active vessel, it will be on rails, and therefore won't
            // be able to have "encounters" via its patchedConicSolver.
            if (vessel.patchedConicSolver == null)
                return "None";

            foreach (var patch in vessel.patchedConicSolver.flightPlan)
            {
                if (patch.patchStartTransition == Orbit.PatchTransitionType.ENCOUNTER)
                {
                    return new OrbitInfo(patch, sharedObj);
                }
            }

            return "None";
        }

        public static KSPActionParam makeActionParam(bool state) // just use this to when you need to call action that requires bool input for direction
        {
            if (state) { return new KSPActionParam(0, KSPActionType.Activate); }
            else { return new KSPActionParam(0, KSPActionType.Deactivate); }
        }

        public static void LandingLegsCtrl(Vessel vessel, bool state)
        {
            vessel.rootPart.SendEvent(state ? "LowerLeg" : "RaiseLeg"); //legacy

            foreach (var p in vessel.parts)
            {
                var geardeploy = p.FindModulesImplementing<ModuleWheels.ModuleWheelDeployment>();
                foreach (var gd in geardeploy)
                {
                    var gear = p.Modules[gd.baseModuleIndex] as ModuleWheelBase;
                    if ((gear != null) && (gear.wheelType == WheelType.LEG)) // identify leg
                    {
                        gd.ActionToggle(makeActionParam(state));
                    }
                }
            }
        }

        public static object GetLandingLegStatus(Vessel vessel)
        {
            var atLeastOneLeg = false; // No legs at all? Always return false

            foreach (var p in vessel.parts)
            {
                var gearlist = p.FindModulesImplementing<ModuleWheelBase>();
                if (gearlist.Count > 0)
                {
                    foreach (var gear in gearlist)
                    {
                        if (gear.wheelType == WheelType.LEG)
                        {
                            atLeastOneLeg = true;
                            var geardeploy = p.FindModulesImplementing<ModuleWheels.ModuleWheelDeployment>();
                            foreach (var gd in geardeploy)
                            {
                                if ((p.Modules[gd.baseModuleIndex] == gear) && (gd.fsm.CurrentState != gd.st_deployed)) //state string is unreliable - may be just empty
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
                else if (p.Modules.OfType<ModuleLandingLeg>().Any()) //Legacy
                {
                    atLeastOneLeg = true;

                    var legs = p.FindModulesImplementing<ModuleLandingLeg>();

                    if (legs.Any(l => l.savedLegState != (int)(ModuleLandingLeg.LegStates.DEPLOYED)))
                    {
                        return false;
                    }
                }
            }
            return atLeastOneLeg;
        }

        public static object GetChuteStatus(Vessel vessel)
        {
            var atLeastOneChute = false; // No chutes at all? Always return false

            foreach (var p in vessel.parts)
            {
                foreach (var c in p.FindModulesImplementing<ModuleParachute>())
                {
                    atLeastOneChute = true;

                    if (c.deploymentState == ModuleParachute.deploymentStates.STOWED)
                    {
                        // If just one chute is not deployed return false
                        return false;
                    }
                }
            }

            return atLeastOneChute;
        }

        public static void DeployParachutes(Vessel vessel, bool state)
        {
            if (!vessel.mainBody.atmosphere || !state) return;
            foreach (var p in vessel.parts)
            {
                foreach (var c in p.FindModulesImplementing<ModuleParachute>())
                {
                    if (c.deploymentState == ModuleParachute.deploymentStates.STOWED)
                    //&& c.deployAltitude * 3 > vessel.heightFromTerrain)
                    {
                        c.DeployAction(null);
                    }
                }
            }
        }

        public static object GetChuteSafeStatus(Vessel vessel) // returns false only if there are chutes to be safely deployed
        {
            foreach (var p in vessel.parts)
            {
                foreach (var c in p.FindModulesImplementing<ModuleParachute>())
                {
                    if ((c.deploymentState == ModuleParachute.deploymentStates.STOWED) && (c.deploymentSafeState == ModuleParachute.deploymentSafeStates.SAFE))
                    {
                        // If just one chute can be safely deployed return false
                        return false;
                    }
                }
            }

            return true;
        }

        public static void DeployParachutesSafe(Vessel vessel, bool state)
        {
            if (!vessel.mainBody.atmosphere || !state) return;
            foreach (var p in vessel.parts)
            {
                foreach (var c in p.FindModulesImplementing<ModuleParachute>())
                {
                    if ((c.deploymentState == ModuleParachute.deploymentStates.STOWED) && (c.deploymentSafeState == ModuleParachute.deploymentSafeStates.SAFE))
                    {
                        c.DeployAction(null);
                    }
                }
            }
        }

        public static object GetSolarPanelStatus(Vessel vessel)
        {
            var atLeastOneSolarPanel = false; // No panels at all? Always return false

            foreach (var p in vessel.parts)
            {
                foreach (var c in p.FindModulesImplementing<ModuleDeployableSolarPanel>())
                {
                    atLeastOneSolarPanel = true;

                    if (c.panelState == ModuleDeployableSolarPanel.panelStates.RETRACTED)
                    {
                        // If just one panel is not deployed return false
                        return false;
                    }
                }
            }

            return atLeastOneSolarPanel;
        }

        public static void SolarPanelCtrl(Vessel vessel, bool state)
        {
            foreach (var p in vessel.parts)
            {
                foreach (var c in p.FindModulesImplementing<ModuleDeployableSolarPanel>())
                {
                    if (state) { c.Extend(); }
                    else { c.Retract(); }
                }
            }
        }

        public static object GetRadiatorStatus(Vessel vessel)
        {
            var atLeastOneRadiator = false; // No radiators at all? Always return false

            foreach (var p in vessel.parts)
            {
                foreach (var c in p.FindModulesImplementing<ModuleActiveRadiator>())
                {
                    atLeastOneRadiator = true;

                    if (!c.IsCooling)
                    {
                        // If just one radiator is not deployed/activated return false
                        return false;
                    }
                }
            }

            return atLeastOneRadiator;
        }

        public static void RadiatorCtrl(Vessel vessel, bool state)
        {
            foreach (var p in vessel.parts)
            {
                var cl = p.FindModulesImplementing<ModuleActiveRadiator>();
                if (cl.Count > 0)
                {
                    var drl = p.FindModulesImplementing<ModuleDeployableRadiator>();
                    if (drl.Count == 0)
                    {
                        foreach (var c in cl)
                        {
                            //fixed radiators
                            if (state) { c.Activate(); }
                            else { c.Shutdown(); }
                        }
                    }
                    else
                    {
                        foreach (var dr in drl)
                        {
                            //deployable radiators
                            if (state) { dr.Extend(); }
                            else { dr.Retract(); }
                        }
                    }
                }
            }
        }

        public static object GetLadderStatus(Vessel vessel)
        {
            var atLeastOneLadder = false; // No ladders at all? Always return false

            foreach (var p in vessel.parts)
            {
                foreach (var c in p.FindModulesImplementing<RetractableLadder>())
                {
                    atLeastOneLadder = true;

                    if (c.StateName != "Extended")
                    {
                        // If just one ladder is not extended return false
                        return false;
                    }
                }
            }

            return atLeastOneLadder;
        }

        public static void LadderCtrl(Vessel vessel, bool state)
        {
            foreach (var p in vessel.parts)
            {
                foreach (var c in p.FindModulesImplementing<RetractableLadder>())
                {
                    if (state) { c.Extend(); }
                    else { c.Retract(); }
                }
            }
        }

        public static object GetBayStatus(Vessel vessel)
        {
            foreach (var p in vessel.parts)
            {
                foreach (var c in p.FindModulesImplementing<ModuleCargoBay>())
                {
                    var m = p.Modules[c.DeployModuleIndex] as ModuleAnimateGeneric; //apparently, it's referenced by the number
                    if (m != null) //bays have ModuleAnimateGeneric, fairings have their own, but they all use ModuleCargoBay
                    {// if (m.GetScalar != c.closedPosition)
                        if (m.animSwitch == (c.closedPosition != 0))
                        {
                            //even one open bay may be critical, therefore return true if any found
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static void BayCtrl(Vessel vessel, bool state)
        {
            foreach (var p in vessel.parts)
            {
                foreach (var c in p.FindModulesImplementing<ModuleCargoBay>())
                {
                    var m = p.Modules[c.DeployModuleIndex] as ModuleAnimateGeneric; //apparently, it's referenced by the number
                    if (m != null)
                    {
                        if ((m.animSwitch == (c.closedPosition == 0))) //closed/closing
                        {
                            if (state) { m.Toggle(); } //open
                        }
                        else //open/opening
                        {
                            if (!state) { m.Toggle(); } //close
                        }
                    }
                }
            }
        }

        public static object GetDrillDeployStatus(Vessel vessel)
        {
            var atLeastOneDrill = false; // No drills at all? Always return false

            foreach (var p in vessel.parts)
            {
                foreach (var c in p.FindModulesImplementing<ModuleAnimationGroup>())
                {
                    if (c.moduleType == "Drill") //drill animation module
                    {
                        atLeastOneDrill = true;

                        if (!c.isDeployed)
                        {
                            // If just one drill is not extended return false
                            return false;
                        }
                    }
                }
            }
            return atLeastOneDrill;
        }

        public static void DrillDeployCtrl(Vessel vessel, bool state)
        {
            foreach (var p in vessel.parts)
            {
                foreach (var c in p.FindModulesImplementing<ModuleAnimationGroup>())
                {
                    if (c.moduleType == "Drill") //drill animation module
                    {
                        if (state) { c.DeployModule(); }
                        else { c.RetractModule(); }
                    }
                }
            }
        }

        public static object GetDrillStatus(Vessel vessel)
        {
            foreach (var p in vessel.parts)
            {
                foreach (var c in p.FindModulesImplementing<ModuleResourceHarvester>())
                {
                    if (c.IsActivated)
                    {
                        // return true if at least one working - you won't get both modules running at once
                        return true;
                    }
                }
                foreach (var c in p.FindModulesImplementing<ModuleAsteroidDrill>())
                {
                    if (c.IsActivated)
                    {
                        // return true if at least one working - you won't get both modules running at once
                        return true;
                    }
                }
            }
            return false;
        }

        public static void DrillCtrl(Vessel vessel, bool state)
        {
            foreach (var p in vessel.parts)
            {
                //call activate on both modules
                foreach (var c in p.FindModulesImplementing<ModuleResourceHarvester>())
                {
                    if (state) { c.StartResourceConverter(); }
                    else { c.StopResourceConverter(); }
                }
                foreach (var c in p.FindModulesImplementing<ModuleAsteroidDrill>())
                {
                    if (state) { c.StartResourceConverter(); }
                    else { c.StopResourceConverter(); }
                }
            }
        }

        public static object GetFuelCellStatus(Vessel vessel)
        {
            string convID = "Fuel Cell";
            foreach (var p in vessel.parts)
            {
                foreach (var c in p.FindModulesImplementing<ModuleResourceConverter>())
                {
                    if ((c.ConverterName.Contains(convID)) || (c.StartActionName.Contains(convID)) || (c.StopActionName.Contains(convID)))
                    {
                        if (c.IsActivated)
                        {
                            // Converters aren't always run all at once
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static void FuelCellCtrl(Vessel vessel, bool state)
        {
            string convID = "Fuel Cell";
            foreach (var p in vessel.parts)
            {
                foreach (var c in p.FindModulesImplementing<ModuleResourceConverter>())
                {
                    if ((c.ConverterName.Contains(convID)) || (c.StartActionName.Contains(convID)) || (c.StopActionName.Contains(convID)))
                    {
                        if (state) { c.StartResourceConverter(); }
                        else { c.StopResourceConverter(); }
                    }
                }
            }
        }

        public static object GetISRUStatus(Vessel vessel)
        {
            string convID = "ISRU";
            foreach (var p in vessel.parts)
            {
                foreach (var c in p.FindModulesImplementing<ModuleResourceConverter>())
                {
                    if ((c.ConverterName.Contains(convID)) || (c.StartActionName.Contains(convID)) || (c.StopActionName.Contains(convID)))
                    {
                        if (c.IsActivated)
                        {
                            // Converters aren't always run all at once
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static void ISRUCtrl(Vessel vessel, bool state)
        {
            string convID = "ISRU";
            foreach (var p in vessel.parts)
            {
                foreach (var c in p.FindModulesImplementing<ModuleResourceConverter>())
                {
                    if ((c.ConverterName.Contains(convID)) || (c.StartActionName.Contains(convID)) || (c.StopActionName.Contains(convID)))
                    {
                        if (state) { c.StartResourceConverter(); }
                        else { c.StopResourceConverter(); }
                    }
                }
            }
        }

        public static object GetIntakeStatus(Vessel vessel)
        {
            var atLeastOneIntake = false; // No intakes at all? Always return false

            foreach (var p in vessel.parts)
            {
                foreach (var c in p.FindModulesImplementing<ModuleResourceIntake>())
                {
                    atLeastOneIntake = true;

                    if (!c.intakeEnabled)
                    {
                        // If just one intake is not open return false
                        return false;
                    }
                }
            }

            return atLeastOneIntake;
        }

        public static void IntakeCtrl(Vessel vessel, bool state)
        {
            foreach (var p in vessel.parts)
            {
                foreach (var c in p.FindModulesImplementing<ModuleResourceIntake>())
                {
                    if (state) { c.Activate(); }
                    else { c.Deactivate(); }
                }
            }
        }

        private static double GetMassDrag(Vessel vessel)
        {
            return vessel.parts.Aggregate<Part, double>(0,
                                                        (current, p) =>
                                                        current + (p.mass + p.GetResourceMass()) * p.maximum_drag);
        }

        public static float GetDryMass(this Vessel vessel)
        {
            return vessel.parts.Sum(part => part.GetDryMass());
        }

        public static float GetWetMass(this Vessel vessel)
        {
            return vessel.parts.Sum(part => part.GetWetMass());
        }

        public static float GetVesselLatitude(Vessel vessel)
        {
            var retVal = (float)vessel.latitude;

            if (retVal > 90) return 90;
            if (retVal < -90) return -90;

            return retVal;
        }

        public static float GetVesselLongitude(Vessel vessel)
        {
            var retVal = vessel.longitude;

            return (float)Utils.DegreeFix(retVal, -180.0);
        }

        public static void UnsetTarget()
        {
            FlightGlobals.fetch.SetVesselTarget(null);
        }

        public static double GetAvailableThrust(Vessel vessel, double atmPressure = -1.0)
        {
            var thrust = 0.0;

            foreach (var p in vessel.parts)
            {
                foreach (PartModule pm in p.Modules)
                {
                    if (pm.isEnabled && pm is ModuleEngines)
                    {
                        thrust += ModuleEngineAdapter.GetEngineThrust((ModuleEngines)pm, useThrustLimit: true, atmPressure: atmPressure);
                    }
                }
            }

            return thrust;
        }

        public static Direction GetFacing(Vessel vessel)
        {
            var vesselRotation = vessel.ReferenceTransform.rotation;
            Quaternion vesselFacing = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(vesselRotation) * Quaternion.identity);
            return new Direction(vesselFacing);
        }

        public static Direction GetFacing(CelestialBody body)
        {
            var bodyRotation = body.transform.rotation;
            Quaternion bodyFacing = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(bodyRotation) * Quaternion.identity);
            return new Direction(bodyFacing);
        }
    }
}