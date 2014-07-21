using System;
using UnityEngine;
using System.Collections.Generic;
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
            Shared.BindingMgr.AddGetter("ALT_RADAR", cpu => Convert.ToDouble(heightToLand() > 0 ? Mathf.Min((float)heightToLand(), (float)heightToLand()) : (float)heightToLand()));
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
            Shared.BindingMgr.AddSetter("VESSELNAME", delegate(CPU cpu, object value) { Shared.Vessel.vesselName = value.ToString(); });
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

        private double heightToLand()
        {
            double landHeight = 0;
            bool firstRay = true;



            if (FlightGlobals.ActiveVessel.LandedOrSplashed)
            {
                landHeight = 0;
            }
            else if (FlightGlobals.ActiveVessel.altitude - FlightGlobals.ActiveVessel.pqsAltitude > 2400)
            {
                landHeight = FlightGlobals.ActiveVessel.altitude - FlightGlobals.ActiveVessel.pqsAltitude;
            }
            else
            {
                List<Part> partToRay = new List<Part>();
                if (FlightGlobals.ActiveVessel.Parts.Count < 50)
                {
                    partToRay = FlightGlobals.ActiveVessel.Parts;
                }
                else 
                {
                    List<partDist> partHeights = new List<partDist>(); 
                    foreach (Part p in FlightGlobals.ActiveVessel.Parts)
                    {
                        partHeights.Add(new partDist() { prt = p, dist = Vector3.Distance(p.transform.position, FlightGlobals.ActiveVessel.mainBody.position) }); 
                       
                    }
                    partHeights.Sort((i, j) => i.dist.CompareTo(j.dist)); 
                    for (int i = 0; i < 30; i = i + 1)
                    {
                        partToRay.Add(partHeights[i].prt); 
                    }

                }

                foreach (Part p in partToRay)
                {
                    try
                    {
                        if (p.collider.enabled) 
                        {
                            Vector3 partEdge = p.collider.ClosestPointOnBounds(FlightGlobals.currentMainBody.position); 
                            RaycastHit pHit;
                            Ray pRayDown = new Ray(partEdge, FlightGlobals.currentMainBody.position);
                            LayerMask pRayMask = 33792;
                            if (Physics.Raycast(pRayDown, out pHit, (float)(FlightGlobals.ActiveVessel.mainBody.Radius + FlightGlobals.ActiveVessel.altitude), pRayMask)) 
                            {

                                if (firstRay) 
                                {

                                    landHeight = pHit.distance;

                                    firstRay = false;
                                }
                                else
                                {

                                    landHeight = Math.Min(landHeight, pHit.distance);


                                }
                            }
                            else if (!firstRay)
                            {
                                landHeight = FlightGlobals.ActiveVessel.altitude;
                                firstRay = false;
                            }
                        }
                    }
                    catch
                    {
                        landHeight = FlightGlobals.ActiveVessel.altitude;
                        firstRay = false;
                    }

                }
                if (landHeight < 1)
                {
                    landHeight = 1;
                }
            }

            if (FlightGlobals.ActiveVessel.mainBody.ocean)
            {
                if (landHeight > FlightGlobals.ActiveVessel.altitude)
                {
                    landHeight = FlightGlobals.ActiveVessel.altitude;
                }
            }

            return landHeight;
        }

        public class partDist
        {
            public Part prt;
            public float dist;
        }
    }

}
