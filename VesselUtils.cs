using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace kOS
{
    public class VesselUtils
    {
        public static List<Part> GetListOfActivatedEngines(Vessel vessel)
        {
            var retList = new List<Part>();

            foreach (Part part in vessel.Parts)
            {
                foreach (PartModule module in part.Modules)
                {
                    if (module is ModuleEngines)
                    {
                        var engineMod = (ModuleEngines)module;

                        if (engineMod.getIgnitionState)
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
            bool resourceIsFound = false;
            total = 0;
            resourceName = resourceName.ToUpper();

            // Ensure the built-in resource types never produce an error, even if the particular vessel is incapable of carrying them
            if (new[] { "LIQUIDFUEL", "ELECTRICCHARGE", "OXIDIZER", "INTAKEAIR" }.Contains(resourceName)) resourceIsFound = true;

            foreach (Part part in vessel.parts)
            {
                foreach (PartResource resource in part.Resources)
                {
                    if (resource.resourceName.ToUpper() == resourceName)
                    {
                        resourceIsFound = true;
                        total += resource.amount;
                    }
                }
            }

            return resourceIsFound;
        }

        public static double GetResource(Vessel vessel, string resourceName)
        {
            double total = 0;
            resourceName = resourceName.ToUpper();

            foreach (Part part in vessel.parts)
            {
                foreach (PartResource resource in part.Resources)
                {
                    if (resource.resourceName.ToUpper() == resourceName)
                    {
                        total += resource.amount;
                    }
                }
            }

            return total;
        }

        public static double GetMaxThrust(Vessel vessel)
        {
            var thrust = 0.0;
            ModuleEngines e;

            foreach (Part p in vessel.parts)
            {
                foreach (PartModule pm in p.Modules)
                {
                    if (!pm.isEnabled) continue;
                    if (pm is ModuleEngines)
                    {
                        e = (pm as ModuleEngines);
                        if (!e.EngineIgnited) continue;
                        thrust += (double)e.maxThrust;
                    }
                }
            }

            return thrust;
        }

        public static Vessel TryGetVesselByName(String name, Vessel origin)
        {
            foreach (Vessel v in FlightGlobals.Vessels)
            {
                if (v != origin && v.vesselName.ToUpper() == name.ToUpper())
                {
                    return v;
                }
            }

            return null;
        }

        public static CelestialBody GetBodyByName(String name)
        {
            foreach (var body in FlightGlobals.fetch.bodies)
            {
                if (name.ToUpper() == body.name.ToUpper())
                {
                    return body;
                }
            }

            return null;
        }

        public static Vessel GetVesselByName(String name, Vessel origin)
        {
            Vessel vessel = TryGetVesselByName(name, origin);

            if (vessel == null)
            {
                throw new kOSException("Vessel '" + name + "' not found");
            }
            else
            {
                return vessel;
            }
        }

        public static void SetTarget(ITargetable val)
        {
            //if (val is Vessel)
            //{
                FlightGlobals.fetch.SetVesselTarget(val);
            //}
            //else if (val is CelestialBody)
            //{/
                
           // }
        }

        public static double GetCommRange(Vessel vessel)
        {
            double range = 100000;

            foreach (Part part in vessel.parts)
            {
                if (part.partInfo.name == "longAntenna")
                {
                    String status = ((ModuleAnimateGeneric)part.Modules["ModuleAnimateGeneric"]).status;

                    if (status == "Fixed" || status == "Locked")
                    {
                        range += 1000000;
                    }
                }
            }

            foreach (Part part in vessel.parts)
            {
                if (part.partInfo.name == "mediumDishAntenna")
                {   
                String status = ((ModuleAnimateGeneric)part.Modules["ModuleAnimateGeneric"]).status;

                if (status == "Fixed" || status == "Locked")
                {
                    range *= 100;
                }
                }
            }

            foreach (Part part in vessel.parts)
            {
                if (part.partInfo.name == "commDish")
                {
                    String status = ((ModuleAnimateGeneric)part.Modules["ModuleAnimateGeneric"]).status;

                    if (status == "Fixed" || status == "Locked")
                    {
                        range *= 200;
                    }
                }
            }

            return range;
        }

        public static double GetDistanceToKerbinSurface(Vessel vessel)
        {
            foreach (var body in FlightGlobals.fetch.bodies)
            {
                if (body.name.ToUpper() == "KERBIN") return Vector3d.Distance(body.position, vessel.GetWorldPos3D()) - 600000; // Kerbin radius = 600,000
            }

            throw new kOSException("Planet Kerbin not found!");
        }

        public static float AngleDelta(float a, float b)
        {
            var delta = b - a;

            while (delta > 180) delta -= 360;
            while (delta < -180) delta += 360;

            return delta;
        }

        public static float GetHeading(Vessel vessel)
        {
            var up = vessel.upAxis;
            var north = GetNorthVector(vessel);
            var headingQ = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(vessel.GetTransform().rotation) * Quaternion.LookRotation(north, up));

            return headingQ.eulerAngles.y;
        }

        public static float GetVelocityHeading(Vessel vessel)
        {
            var up = vessel.upAxis;
            var north = GetNorthVector(vessel);
            var headingQ = Quaternion.Inverse(Quaternion.Inverse(Quaternion.LookRotation(vessel.srf_velocity, up)) * Quaternion.LookRotation(north, up));

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
            var vector = Vector3d.Exclude(vessel.upAxis, target.GetWorldPos3D() - vessel.GetWorldPos3D()).normalized;
            var headingQ = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(Quaternion.LookRotation(vector, up)) * Quaternion.LookRotation(north, up));

            return headingQ.eulerAngles.y;
        }

        public static Vector3d GetNorthVector(Vessel vessel)
        {
            return Vector3d.Exclude(vessel.upAxis, vessel.mainBody.transform.up);
        }

        public static object TryGetEncounter(Vessel vessel)
        {
            foreach (Orbit patch in vessel.patchedConicSolver.flightPlan)
            {
                if (patch.patchStartTransition == Orbit.PatchTransitionType.ENCOUNTER)
                {
                    return new OrbitInfo(patch);
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
            bool atLeastOneLeg = false; // No legs at all? Always return false

            foreach (Part p in vessel.parts)
            {
                if (p.Modules.OfType<ModuleLandingLeg>().Count() > 0)
                {
                    atLeastOneLeg = true;

                    foreach (ModuleLandingLeg l in p.FindModulesImplementing<ModuleLandingLeg>())
                    {
                        if (l.savedLegState != (int)(ModuleLandingLeg.LegStates.DEPLOYED))
                        {
                            // If just one leg is retracted, still moving, or broken return false.
                            return false;
                        }
                    }
                }
            }

            return atLeastOneLeg;
        }

        public static object GetChuteStatus(Vessel vessel)
        {
            bool atLeastOneChute = false; // No chutes at all? Always return false

            foreach (Part p in vessel.parts)
            {
                foreach (ModuleParachute c in p.FindModulesImplementing<ModuleParachute>())
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
            if (vessel.mainBody.atmosphere && state)
            {
                foreach (Part p in vessel.parts)
                {
                    if (p.Modules.OfType<ModuleParachute>().Count() > 0 && state)
                    {
                        foreach (ModuleParachute c in p.FindModulesImplementing<ModuleParachute>())
                        {
                            if (c.deploymentState == ModuleParachute.deploymentStates.STOWED) //&& c.deployAltitude * 3 > vessel.heightFromTerrain)
                            {
                                c.DeployAction(null);
                            }
                        }
                    }
                }
            }
        }

        public static float GetVesselLattitude(Vessel vessel)
        {
            float retVal = (float)vessel.latitude;

            if (retVal > 90) return 90;
            if (retVal < -90) return -90;

            return retVal;
        }

        public static float GetVesselLongitude(Vessel vessel)
        {
            float retVal = (float)vessel.longitude;

            while (retVal > 180) retVal -= 360;
            while (retVal < -180) retVal += 360;

            return retVal;
        }
    }
}
