using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Text.RegularExpressions;

namespace kOS
{
    
    [CommandAttribute(@"^STAGE$")]
    class CommandVesselStage : Command
    {
        public CommandVesselStage(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            Staging.ActivateNextStage();

            State = ExecutionState.DONE;
        }
    }

    [CommandAttribute(@"^LIST (PARTS|RESOURCES|ENGINES|TARGETS|BODIES)$")]
    class CommandVesselListings : Command
    {
        public CommandVesselListings(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            switch (RegexMatch.Groups[1].Value.ToUpper())
            {
                    
                case "BODIES":
                    StdOut("");
                    StdOut("Vessel Name");
                    StdOut("-------------------------------------");
                    foreach (var body in FlightGlobals.fetch.bodies)
                    {
                        StdOut(body.bodyName);
                    }
                    StdOut("");

                    break;
                

                case "TARGETS":
                    StdOut("");
                    StdOut("Vessel Name              Distance");
                    StdOut("-------------------------------------");

                    float commRange = VesselUtils.GetCommRange(Vessel);

                    foreach (Vessel vessel in FlightGlobals.Vessels)
                    {
                        if (vessel != Vessel)
                        {
                            var vT = new VesselTarget(vessel, this);
                            if (vT.IsInRange(commRange))
                            {
                                StdOut(vT.target.vesselName.PadRight(24) + " " + vT.GetDistance().ToString("0.0").PadLeft(8));
                            }
                        }
                    }

                    StdOut("");

                    break;

                case "RESOURCES":
                    StdOut("");
                    StdOut("Stage      Resource Name               Amount");
                    StdOut("------------------------------------------------");

                    foreach (Part part in Vessel.Parts)
                    {
                        String stageStr = part.inverseStage.ToString();

                        foreach (PartResource resource in part.Resources)
                        {
                            StdOut(part.inverseStage.ToString() + " " + resource.resourceName.PadRight(20) + " " + resource.amount.ToString("0.00").PadLeft(8));
                        }
                    }
                    break;

                case "PARTS":
                    StdOut("------------------------------------------------");

                    foreach (Part part in Vessel.Parts)
                    {
                        StdOut(part.ConstructID + "  " + part.missionID + " " + part.partInfo.name);
                    }

                    break;

                case "ENGINES":
                    StdOut("------------------------------------------------");

                    foreach (Part part in VesselUtils.GetListOfActivatedEngines(Vessel))
                    {
                        foreach (PartModule module in part.Modules)
                        {
                            if (module is ModuleEngines)
                            {
                                var engineMod = (ModuleEngines)module;
                                
                                StdOut(part.uid + "  " + part.inverseStage.ToString() + " " + engineMod.moduleName);
                            }
                        }
                    }

                    break;
            }

            State = ExecutionState.DONE;
        }
    }
}