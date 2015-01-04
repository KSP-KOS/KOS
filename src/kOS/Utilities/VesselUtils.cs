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
                    list = ElementValue.PartsToList(partList);
                    break;

                case "DOCKINGPORTS":
                    list = DockingPortValue.PartsToList(partList, sharedObj);
                    break;
            }
            return list;
        }

        public static double GetMaxThrust(Vessel vessel)
        {
            var thrust = 0.0;

            foreach (var p in vessel.parts)
            {
                foreach (PartModule pm in p.Modules)
                {
                    if (!pm.isEnabled) continue;
                    if (!(pm is ModuleEngines || pm is ModuleEnginesFX)) continue;

                    var engine = pm as ModuleEngines;
                    var enginefx = pm as ModuleEnginesFX;

                    if (enginefx != null)
                    {
                        if (!enginefx.isOperational) continue;
                        thrust += enginefx.maxThrust;
                    }

                    if (engine != null)
                    {
                        if (!engine.isOperational) continue;
                        thrust += engine.maxThrust;
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
            foreach (var patch in vessel.patchedConicSolver.flightPlan)
            {
                if (patch.patchStartTransition == Orbit.PatchTransitionType.ENCOUNTER)
                {
                    return new OrbitInfo(patch, sharedObj);
                }
            }

            return "None";
        }

        public static void LandingLegsCtrl(Vessel vessel, bool state)
        {
            // This appears to work on all legs in 0.22
            vessel.rootPart.SendEvent(state ? "LowerLeg" : "RaiseLeg");
        }

        internal static object GetLandingLegStatus(Vessel vessel)
        {
            var atLeastOneLeg = false; // No legs at all? Always return false

            foreach (var p in vessel.parts)
            {
                if (!p.Modules.OfType<ModuleLandingLeg>().Any()) continue;
                atLeastOneLeg = true;

                var legs = p.FindModulesImplementing<ModuleLandingLeg>();

                if (legs.Any(l => l.savedLegState != (int)(ModuleLandingLeg.LegStates.DEPLOYED)))
                {
                    return false;
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
                if (!p.Modules.OfType<ModuleParachute>().Any()) continue;
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
            vessel.rootPart.SendEvent(state ? "Extend" : "Retract");
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

        private static double RealMaxAtmosphereAltitude(CelestialBody body)
        {
            // This comes from MechJeb CelestialBodyExtensions.cs
            if (!body.atmosphere) return 0;
            //Atmosphere actually cuts out when exp(-altitude / scale height) = 1e-6
            return -body.atmosphereScaleHeight * 1000 * Math.Log(1e-6);
        }

        public static double GetTerminalVelocity(Vessel vessel)
        {
            if (vessel.mainBody.GetAltitude(vessel.findWorldCenterOfMass()) > RealMaxAtmosphereAltitude(vessel.mainBody))
                return double.PositiveInfinity;
            double densityOfAir =
                FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(vessel.findWorldCenterOfMass(),
                                                                            vessel.mainBody));
            return
                Math.Sqrt(2 * FlightGlobals.getGeeForceAtPosition(vessel.findWorldCenterOfMass()).magnitude *
                          vessel.GetTotalMass() / (GetMassDrag(vessel) * FlightGlobals.DragMultiplier * densityOfAir));
        }

        public static float GetVesselLattitude(Vessel vessel)
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

        public static double GetAvailableThrust(Vessel vessel)
        {
            var thrust = 0.0;

            foreach (var p in vessel.parts)
            {
                foreach (PartModule pm in p.Modules)
                {
                    if (!pm.isEnabled) continue;
                    if (!(pm is ModuleEngines || pm is ModuleEnginesFX)) continue;

                    var engine = pm as ModuleEngines;
                    var enginefx = pm as ModuleEnginesFX;

                    if (enginefx != null)
                    {
                        if (!enginefx.isOperational) continue;
                        thrust += enginefx.maxThrust * enginefx.thrustPercentage / 100;
                    }

                    if (engine != null)
                    {
                        if (!engine.isOperational) continue;
                        thrust += engine.maxThrust * engine.thrustPercentage / 100;
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
    }
}