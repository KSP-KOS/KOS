using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace kOS
{

    [kOSBinding("ksp")]
    public class BindingsFlightStats : Binding
    {
        public override void AddTo(BindingManager manager)
        {
            manager.AddGetter("VESSELNAME",     delegate(CPU cpu) { return cpu.Vessel.vesselName; });
            manager.AddSetter("VESSELNAME",     delegate(CPU cpu, object value ) { cpu.Vessel.vesselName = value.ToString(); });

            manager.AddGetter("ALTITUDE",       delegate(CPU cpu) { return (float)cpu.Vessel.altitude; });
            manager.AddGetter("ALT:RADAR",      delegate(CPU cpu) { return (float)cpu.Vessel.heightFromTerrain; });
            manager.AddGetter("MISSIONTIME",    delegate(CPU cpu) { return (float)cpu.Vessel.missionTime; });
            manager.AddGetter("STATUS",         delegate(CPU cpu) { return cpu.Vessel.situation.ToString().Replace("_", " "); });
			manager.AddGetter("COMMRANGE",      delegate(CPU cpu) { return (float)VesselUtils.GetCommRange(cpu.Vessel); });
			manager.AddGetter("INCOMMRANGE",    delegate(CPU cpu) { return Convert.ToSingle(CheckCommRange(cpu.Vessel)); });
            manager.AddGetter("APOAPSIS",       delegate(CPU cpu) { return (float)cpu.Vessel.orbit.ApA; });
            manager.AddGetter("PERIAPSIS",      delegate(CPU cpu) { return (float)cpu.Vessel.orbit.PeA; });

            manager.AddGetter("ALT:APOAPSIS",   delegate(CPU cpu) { return (float)cpu.Vessel.orbit.ApA; });
            manager.AddGetter("ALT:PERIAPSIS",  delegate(CPU cpu) { return (float)cpu.Vessel.orbit.PeA; });
            manager.AddGetter("ETA:APOAPSIS",   delegate(CPU cpu) { return (float)cpu.Vessel.orbit.timeToAp; });
            manager.AddGetter("ETA:PERIAPSIS",  delegate(CPU cpu) { return (float)cpu.Vessel.orbit.timeToPe; });

            manager.AddGetter("VELOCITY",       delegate(CPU cpu) { return new VesselVelocity(cpu.Vessel); });

            manager.AddGetter("ANGULARMOMENTUM",delegate(CPU cpu) { return new Direction(cpu.Vessel.angularMomentum, true); });
            manager.AddGetter("ANGULARVEL",     delegate(CPU cpu) { return new Direction(cpu.Vessel.angularVelocity, true); });
            manager.AddGetter("MASS",           delegate(CPU cpu) { return cpu.Vessel.GetTotalMass(); });
            manager.AddGetter("VERTICALSPEED",  delegate(CPU cpu) { return (float)cpu.Vessel.verticalSpeed; });
            manager.AddGetter("SURFACESPEED",   delegate(CPU cpu) { return (float)cpu.Vessel.horizontalSrfSpeed; });
           
            manager.AddGetter("BODY",           delegate(CPU cpu) { return cpu.Vessel.mainBody.bodyName; });
            manager.AddGetter("LATITUDE",       delegate(CPU cpu) { return (float)getLattitude(cpu); });
            manager.AddGetter("LONGITUDE",      delegate(CPU cpu) { return (float)getLongitude(cpu); });
            manager.AddGetter("GEOPOSITION",    delegate(CPU cpu) { return new GeoCoordinates(cpu.Vessel, getLattitude(cpu), getLongitude(cpu)); });

            manager.AddGetter("HEADING",        delegate(CPU cpu) { return VesselUtils.GetHeading(cpu.Vessel); });

            manager.AddGetter("NORTH",          delegate(CPU cpu) { return new Direction(VesselUtils.GetNorthVector(cpu.Vessel), false); });
            manager.AddGetter("UP",             delegate(CPU cpu) { return new Direction(cpu.Vessel.upAxis, false); });

            manager.AddGetter("NODE",           delegate(CPU cpu) {
                var vessel = cpu.Vessel;
                if (!vessel.patchedConicSolver.maneuverNodes.Any())
                {
                    throw new kOSException("No maneuver nodes present!");
                }
                var up = (vessel.findLocalMOI(vessel.findWorldCenterOfMass()) - vessel.mainBody.position).normalized;
                var fwd = vessel.patchedConicSolver.maneuverNodes[0].GetBurnVector(cpu.Vessel.orbit);
                var rotRef = Quaternion.LookRotation(fwd, up);

                Direction d = new Direction();
                d.Rotation = rotRef;
                return d;
            });

            manager.AddGetter("MAG:NODE", delegate(CPU cpu) {
                var vessel = cpu.Vessel;
                var orbit = vessel.orbit;
                if (!vessel.patchedConicSolver.maneuverNodes.Any())
                {
                    throw new kOSException("No maneuver nodes present!");
                }
                var mag = vessel.patchedConicSolver.maneuverNodes[0].GetBurnVector(orbit).magnitude;

                return (float)mag;
            });

            manager.AddGetter("ETA:NODE", delegate(CPU cpu) {
                var vessel = cpu.Vessel;
                if (!vessel.patchedConicSolver.maneuverNodes.Any())
                {
                    throw new kOSException("No maneuver nodes present!");
                }
                var time = vessel.patchedConicSolver.maneuverNodes[0].UT;
                var currTime = Planetarium.GetUniversalTime();

                return (float)(time - currTime);
            });

            manager.AddGetter("PROGRADE",       delegate(CPU cpu)
            {
                var vessel = cpu.Vessel;
                var up = (vessel.findLocalMOI(vessel.findWorldCenterOfMass()) - vessel.mainBody.position).normalized;

                Direction d = new Direction();
                d.Rotation = Quaternion.LookRotation(cpu.Vessel.orbit.GetVel().normalized, up);
                return d;
            });

            manager.AddGetter("RETROGRADE",     delegate(CPU cpu)
            {
                var vessel = cpu.Vessel;
                var up = (vessel.findLocalMOI(vessel.findWorldCenterOfMass()) - vessel.mainBody.position).normalized;

                Direction d = new Direction();
                var vesselRoll = cpu.Vessel.GetTransform().eulerAngles.y;
                d.Rotation = Quaternion.LookRotation(cpu.Vessel.orbit.GetVel().normalized * -1, up);
                return d;
            });

            manager.AddGetter("FACING",         delegate(CPU cpu)
            {
                var facing = cpu.Vessel.transform.up;
                return new Direction(new Vector3d(facing.x, facing.y, facing.z).normalized, false);
            });

            manager.AddGetter("MAXTHRUST",      delegate(CPU cpu)
            {
                return VesselUtils.GetMaxThrust(cpu.Vessel);
            });

            manager.AddGetter("AV", delegate(CPU cpu) { return cpu.Vessel.transform.InverseTransformDirection(cpu.Vessel.rigidbody.angularVelocity); });

            manager.AddGetter("STAGE",          delegate(CPU cpu) { return new StageValues(cpu.Vessel); });

        }

        private static float getLattitude(CPU cpu)
        {
            float retVal = (float)cpu.Vessel.latitude;

            if (retVal > 90) return 90;
            if (retVal < -90) return -90;
                
            return retVal;
        }

        private static float getLongitude(CPU cpu)
        {
            float retVal = (float)cpu.Vessel.longitude;

            while (retVal > 180) retVal -= 360;
            while (retVal < -180) retVal += 360;

            return retVal;
        }

		private static bool CheckCommRange(Vessel vessel)
		{
			return (VesselUtils.GetDistanceToKerbinSurface(vessel) < VesselUtils.GetCommRange(vessel));
		}
    }
}
