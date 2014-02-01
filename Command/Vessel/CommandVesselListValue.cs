using System.Collections.Generic;
using System.Text.RegularExpressions;
using kOS.Context;
using kOS.Expression;
using kOS.Suffixed;

namespace kOS.Command.Vessel
{
    [Command(@"^LIST (PARTS|RESOURCES|ENGINES|TARGETS|BODIES|SENSORS|ELEMENTS) IN *")]
    internal class CommandVesselListValue : Command
    {
        public CommandVesselListValue(Match regexMatch, IExecutionContext context) : base(regexMatch, context)
        {
        }

        public override void Evaluate()
        {
            var name = RegexMatch.Groups[2].Value;
            var type = new Term(RegexMatch.Groups[1].Value);
            var list = new ListValue();

            var partList = new List<Part>();
            foreach (var part in Vessel.Parts)
            {
                partList.Add(part);
            }

            switch (type.Text)
            {
                case "BODIES":
                    foreach (var body in FlightGlobals.fetch.bodies)
                    {
                        list.Add(new BodyTarget(body, Vessel));
                    }
                    break;
                case "TARGETS":
                    foreach (var vessel in FlightGlobals.Vessels)
                    {
                        if (vessel == Vessel) continue;
                        list.Add(new VesselTarget(vessel, ParentContext));
                    }
                    break;
                case "RESOURCES":
                    list = ResourceValue.PartsToList(partList);
                    break;
                case "PARTS":
                    list = PartValue.PartsToList(partList);
                    break;
                case "ENGINES":
                    list = EngineValue.PartsToList(partList);
                    UnityEngine.Debug.Log("kOS: " + partList.Count + " " + list.Count);
                    break;
                case "SENSORS":
                    list = SensorValue.PartsToList(partList);
                    break;
                case "ELEMENTS":
                    list = ElementValue.PartsToList(partList);
                    break;
            }

            FindOrCreateVariable(name).Value = list;

            State = ExecutionState.DONE;
        }
    }
}