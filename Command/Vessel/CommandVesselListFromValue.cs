using System.Text.RegularExpressions;
using kOS.Context;
using kOS.Expression;
using kOS.Suffixed;
using kOS.Utilities;

namespace kOS.Command.Vessel
{
    [Command("LIST /_ FROM /_ IN /_")]
    internal class CommandVesselListFromValue : Command
    {
        public CommandVesselListFromValue(Match regexMatch, IExecutionContext context) : base(regexMatch, context)
        {
        }

        public override void Evaluate()
        {
            var target = new Term(RegexMatch.Groups[2].Value);
            var name = new Term(RegexMatch.Groups[3].Value);
            var type = new Term(RegexMatch.Groups[1].Value);

            var variable = FindVariable(target.Text);
            if (!(variable.Value is VesselTarget)) return;

            var list = Vessel.PartList(type.Text);
            FindOrCreateVariable(name.Text).Value = list;
            State = ExecutionState.DONE;
        }
    }
}