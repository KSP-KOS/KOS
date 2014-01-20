using System.Globalization;
using System.Text.RegularExpressions;
using kOS.Context;
using kOS.Utilities;
using kOS.Value;

namespace kOS.Command.Vessel
{
    [Command(@"^LIST (PARTS|RESOURCES|ENGINES|TARGETS|BODIES|SENSORS)$")]
    class CommandVesselListings : Command
    {
        public CommandVesselListings(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            switch (RegexMatch.Groups[1].Value.ToUpper())
            {
                case "BODIES":
                    StdOut("");
                    StdOut("Name           Distance");
                    StdOut("-------------------------------------");
                    foreach (var body in FlightGlobals.fetch.bodies)
                    {
                        StdOut(body.bodyName.PadLeft(14) + " " + Vector3d.Distance(body.position, Vessel.GetWorldPos3D()));
                    }
                    StdOut("");

                    break;
                

                case "TARGETS":
                    StdOut("");
                    StdOut("Vessel Name              Distance");
                    StdOut("-------------------------------------");

                    var commRange = VesselUtils.GetCommRange(Vessel);

                    foreach (var vessel in FlightGlobals.Vessels)
                    {
                        if (vessel == Vessel) continue;
                        var vT = new VesselTarget(vessel, this);
                        if (vT.IsInRange(commRange))
                        {
                            StdOut(vT.Target.vesselName.PadRight(24) + " " + vT.GetDistance().ToString("0.0").PadLeft(8));
                        }
                    }

                    StdOut("");

                    break;

                case "RESOURCES":
                    StdOut("");
                    StdOut("Stage      Resource Name               Amount");
                    StdOut("------------------------------------------------");

                    foreach (var part in Vessel.Parts)
                    {
                        foreach (PartResource resource in part.Resources)
                        {
                            StdOut(part.inverseStage.ToString(CultureInfo.InvariantCulture) + " " + resource.resourceName.PadRight(20) + " " + resource.amount.ToString("0.00").PadLeft(8));
                        }
                    }
                    break;

                case "PARTS":
                    StdOut("------------------------------------------------");

                    foreach (var part in Vessel.Parts)
                    {
                        StdOut(part.ConstructID + " " + part.partInfo.name);
                    }

                    break;

                case "ENGINES":
                    StdOut("------------------------------------------------");

                    foreach (var part in VesselUtils.GetListOfActivatedEngines(Vessel))
                    {
                        foreach (PartModule module in part.Modules)
                        {
                            if (!(module is ModuleEngines)) continue;
                            var engineMod = (ModuleEngines)module;
                                
                            StdOut(part.uid + "  " + part.inverseStage.ToString(CultureInfo.InvariantCulture) + " " + engineMod.moduleName);
                        }
                    }

                    break;

                case "SENSORS":
                    StdOut("");
                    StdOut("Part Name                             Sensor Type");
                    StdOut("------------------------------------------------");

                    foreach (var part in Vessel.Parts)
                    {
                        foreach (PartModule module in part.Modules)
                        {
                            var sensor = module as ModuleEnviroSensor;
                            if (sensor == null) continue;
                            if (part.partInfo.name.Length > 37)
                                StdOut(part.partInfo.title.PadRight(34) + "... " + sensor.sensorType);
                            else
                                StdOut(part.partInfo.title.PadRight(37) + " " + sensor.sensorType);
                        }
                    }

                    break;
            }

            State = ExecutionState.DONE;
        }
    }
}
