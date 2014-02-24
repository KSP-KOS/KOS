using System.Text.RegularExpressions;
using kOS.Context;
using kOS.Expression;
using kOS.Suffixed;
using kOS.Utilities;

namespace kOS.Command.Vessel
{
    [Command("LIST /_ IN /_")]
    internal class CommandVesselListValue : Command
    {
        public CommandVesselListValue(Match regexMatch, IExecutionContext context) : base(regexMatch, context)
        {
        }

        public override void Evaluate()
        {
            var name = new Term(RegexMatch.Groups[2].Value);
            var type = new Term(RegexMatch.Groups[1].Value);
            var list = new ListValue();

            switch (type.Text.ToUpper())
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
            }

            if (list.Empty())
            {
                list = Vessel.PartList(type.Text);
            }

            FindOrCreateVariable(name.Text).Value = list;

            State = ExecutionState.DONE;
        }

    }
}