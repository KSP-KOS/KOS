﻿using System;
using UnityEngine;
using kOS.Safe.Exceptions;
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
            Shared = shared;

            Shared.BindingMgr.AddGetter("ALT_APOAPSIS", cpu => Shared.Vessel.orbit.ApA);
            Shared.BindingMgr.AddGetter("ALT_PERIAPSIS", cpu => Shared.Vessel.orbit.PeA);
            Shared.BindingMgr.AddGetter("ALT_RADAR", cpu => Convert.ToDouble(Shared.Vessel.heightFromTerrain > 0 ? Mathf.Min(Shared.Vessel.heightFromTerrain, (float)Shared.Vessel.altitude) : (float)Shared.Vessel.altitude));
            Shared.BindingMgr.AddGetter("ANGULARVELOCITY", cpu => Shared.Vessel.transform.InverseTransformDirection(Shared.Vessel.rigidbody.angularVelocity));
            Shared.BindingMgr.AddGetter("COMMRANGE", cpu => int.MaxValue);
            Shared.BindingMgr.AddGetter("ENCOUNTER", cpu => VesselUtils.TryGetEncounter(Shared.Vessel,Shared));
            Shared.BindingMgr.AddGetter("ETA_APOAPSIS", cpu => Shared.Vessel.orbit.timeToAp);
            Shared.BindingMgr.AddGetter("ETA_PERIAPSIS", cpu => Shared.Vessel.orbit.timeToPe);
            Shared.BindingMgr.AddGetter("ETA_TRANSITION", cpu => Shared.Vessel.orbit.EndUT - Planetarium.GetUniversalTime());
            Shared.BindingMgr.AddGetter("INCOMMRANGE", cpu => true);
            Shared.BindingMgr.AddGetter("MISSIONTIME", cpu => Shared.Vessel.missionTime);
            Shared.BindingMgr.AddGetter("OBT", cpu => new OrbitInfo(Shared.Vessel.orbit,Shared));
            Shared.BindingMgr.AddGetter("TIME", cpu => new TimeSpan(Planetarium.GetUniversalTime()));
            Shared.BindingMgr.AddGetter("SHIP", cpu => new VesselTarget(Shared));
            Shared.BindingMgr.AddGetter("ACTIVESHIP", cpu => new VesselTarget(FlightGlobals.ActiveVessel, Shared));
            Shared.BindingMgr.AddGetter("STATUS", cpu => Shared.Vessel.situation.ToString());
            Shared.BindingMgr.AddGetter("STAGE", cpu => new StageValues(Shared.Vessel));

            //DEPRICATED VESSELNAME
            Shared.BindingMgr.AddSetter("VESSELNAME", delegate { throw new KOSException("VESSELNAME is DEPRICATED, use SHIPNAME.");});
            Shared.BindingMgr.AddSetter("SHIPNAME", delegate(CPU cpu, object value) { Shared.Vessel.vesselName = value.ToString(); });

            Shared.BindingMgr.AddGetter("NEXTNODE", delegate
                {
                    var vessel = Shared.Vessel;
                    if (vessel.patchedConicSolver.maneuverNodes.Count == 0)
                    {
                        throw new Exception("No maneuver nodes present!");
                    }

                    return Node.FromExisting(vessel, vessel.patchedConicSolver.maneuverNodes[0], Shared);
                });

            // These are now considered shortcuts to SHIP:suffix
            foreach (var scName in VesselTarget.ShortCuttableShipSuffixes)
            {
                var cName = scName;
                Shared.BindingMgr.AddGetter(scName, cpu => new VesselTarget(Shared).GetSuffix(cName));
            }
        }
    }
}
