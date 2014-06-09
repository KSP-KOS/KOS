using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using kOS.Suffixed;
using kOS.Utilities;
using kOS.Execution;
using TimeSpan = kOS.Suffixed.TimeSpan;

namespace kOS.Binding
{
    [kOSBinding("ksp")]
    public class FlightStats : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            _shared = shared;

            _shared.BindingMgr.AddGetter("ALT_APOAPSIS", cpu => _shared.Vessel.orbit.ApA);
            _shared.BindingMgr.AddGetter("ALT_PERIAPSIS", cpu => _shared.Vessel.orbit.PeA);
            _shared.BindingMgr.AddGetter("ALT_RADAR", cpu => _shared.Vessel.heightFromTerrain > 0 ? Mathf.Min(_shared.Vessel.heightFromTerrain, (float)_shared.Vessel.altitude) : (float)_shared.Vessel.altitude);
            _shared.BindingMgr.AddGetter("ANGULARVELOCITY", cpu => _shared.Vessel.transform.InverseTransformDirection(_shared.Vessel.rigidbody.angularVelocity));
            _shared.BindingMgr.AddGetter("COMMRANGE", cpu => VesselUtils.GetCommRange(_shared.Vessel));
            _shared.BindingMgr.AddGetter("ENCOUNTER", cpu => VesselUtils.TryGetEncounter(_shared.Vessel,_shared));
            _shared.BindingMgr.AddGetter("ETA_APOAPSIS", cpu => _shared.Vessel.orbit.timeToAp);
            _shared.BindingMgr.AddGetter("ETA_PERIAPSIS", cpu => _shared.Vessel.orbit.timeToPe);
            _shared.BindingMgr.AddGetter("ETA_TRANSITION", cpu => _shared.Vessel.orbit.EndUT - Planetarium.GetUniversalTime());
            _shared.BindingMgr.AddGetter("INCOMMRANGE", cpu => Convert.ToDouble(CheckCommRange(_shared.Vessel)));
            _shared.BindingMgr.AddGetter("MISSIONTIME", cpu => _shared.Vessel.missionTime);
            _shared.BindingMgr.AddGetter("OBT", cpu => new OrbitInfo( _shared.Vessel.orbit,_shared));
            _shared.BindingMgr.AddGetter("TIME", cpu => new TimeSpan(Planetarium.GetUniversalTime()));
            _shared.BindingMgr.AddGetter("SHIP", cpu => new VesselTarget(_shared));
            _shared.BindingMgr.AddGetter("STATUS", cpu => _shared.Vessel.situation.ToString());
            _shared.BindingMgr.AddGetter("STAGE", cpu => new StageValues(_shared.Vessel));
            _shared.BindingMgr.AddSetter("VESSELNAME", delegate(CPU cpu, object value) { _shared.Vessel.vesselName = value.ToString(); });

            _shared.BindingMgr.AddGetter("NEXTNODE", delegate(CPU cpu)
                {
                    var vessel = _shared.Vessel;
                    if (vessel.patchedConicSolver.maneuverNodes.Count == 0)
                    {
                        throw new Exception("No maneuver nodes present!");
                    }

                    return Node.FromExisting(vessel, vessel.patchedConicSolver.maneuverNodes[0], shared);
                });

            // These are now considered shortcuts to SHIP:suffix
            foreach (var scName in VesselTarget.ShortCuttableShipSuffixes)
            {
                var cName = scName;
                _shared.BindingMgr.AddGetter(scName, cpu => new VesselTarget(_shared).GetSuffix(cName));
            }
        }
            
        private static bool CheckCommRange(Vessel vessel)
        {
            return (VesselUtils.GetDistanceToHome(vessel) < VesselUtils.GetCommRange(vessel));
        }
    }
}
