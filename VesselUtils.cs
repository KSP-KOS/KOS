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
            FlightGlobals.fetch.SetVesselTarget(val);
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
    }
}
