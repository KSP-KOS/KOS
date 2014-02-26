using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using kOS.Suffixed;
using kOS.Utilities;
using kOS.Execution;

namespace kOS.Binding
{
    [kOSBinding("ksp")]
    public class FlightStats : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            _shared = shared;

            _shared.BindingMgr.AddGetter("ALT|RADAR",      delegate(CPU cpu) { return _shared.Vessel.heightFromTerrain > 0 ? Mathf.Min(_shared.Vessel.heightFromTerrain, (float)_shared.Vessel.altitude) : (float)_shared.Vessel.altitude; });
            _shared.BindingMgr.AddGetter("ALT|APOAPSIS",   delegate(CPU cpu) { return _shared.Vessel.orbit.ApA; });
            _shared.BindingMgr.AddGetter("ALT|PERIAPSIS",  delegate(CPU cpu) { return _shared.Vessel.orbit.PeA; });
            _shared.BindingMgr.AddGetter("ETA|APOAPSIS",   delegate(CPU cpu) { return _shared.Vessel.orbit.timeToAp; });
            _shared.BindingMgr.AddGetter("ETA|PERIAPSIS",  delegate(CPU cpu) { return _shared.Vessel.orbit.timeToPe; });

            _shared.BindingMgr.AddGetter("MISSIONTIME",    delegate(CPU cpu) { return _shared.Vessel.missionTime; });
            _shared.BindingMgr.AddGetter("TIME",           delegate(CPU cpu) { return new kOS.Suffixed.TimeSpan(Planetarium.GetUniversalTime()); });

            _shared.BindingMgr.AddGetter("STATUS",         delegate(CPU cpu) { return _shared.Vessel.situation.ToString().Replace("_", " "); });
            _shared.BindingMgr.AddGetter("COMMRANGE",      delegate(CPU cpu) { return VesselUtils.GetCommRange(_shared.Vessel); });
            _shared.BindingMgr.AddGetter("INCOMMRANGE",    delegate(CPU cpu) { return Convert.ToDouble(CheckCommRange(_shared.Vessel)); });

            _shared.BindingMgr.AddGetter("AV", delegate(CPU cpu) { return _shared.Vessel.transform.InverseTransformDirection(_shared.Vessel.rigidbody.angularVelocity); });
            _shared.BindingMgr.AddGetter("STAGE", delegate(CPU cpu) { return new StageValues(_shared.Vessel); });

            _shared.BindingMgr.AddGetter("ENCOUNTER",      delegate(CPU cpu) { return VesselUtils.TryGetEncounter(_shared.Vessel); });

            _shared.BindingMgr.AddGetter("NEXTNODE",       delegate(CPU cpu)
            {
                var vessel = _shared.Vessel;
                if (!vessel.patchedConicSolver.maneuverNodes.Any()) { throw new Exception("No maneuver nodes present!"); }

                return Node.FromExisting(vessel, vessel.patchedConicSolver.maneuverNodes[0]);
            });

            // Things like altitude, mass, maxthrust are now handled the same for other ships as the current ship
            _shared.BindingMgr.AddGetter("SHIP", delegate(CPU cpu) { return new VesselTarget(_shared.Vessel, _shared.Vessel); });

            // These are now considered shortcuts to SHIP:suffix
            foreach (string scName in VesselTarget.ShortCuttableShipSuffixes)
            {
                string cName = scName;
                _shared.BindingMgr.AddGetter(scName, delegate(CPU cpu) { return new VesselTarget(_shared.Vessel, _shared.Vessel).GetSuffix(cName); });
            }

            _shared.BindingMgr.AddSetter("VESSELNAME", delegate(CPU cpu, object value) { _shared.Vessel.vesselName = value.ToString(); });
            }

            //private float getLattitude(CPU cpu)
            //{
            //    float retVal = (float)_shared.Vessel.latitude;

            //    if (retVal > 90) return 90;
            //    if (retVal < -90) return -90;

            //    return retVal;
            //}

            //private float getLongitude(CPU cpu)
            //{
            //    float retVal = (float)_shared.Vessel.longitude;

            //    while (retVal > 180) retVal -= 360;
            //    while (retVal < -180) retVal += 360;

            //    return retVal;
            //}

            private static bool CheckCommRange(Vessel vessel)
            {
                return (VesselUtils.GetDistanceToKerbinSurface(vessel) < VesselUtils.GetCommRange(vessel));
            }
  }
}
