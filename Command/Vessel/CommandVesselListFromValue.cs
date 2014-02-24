using System.Text.RegularExpressions;
using kOS.Context;
using kOS.Debug;
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
            var target = new Expression.Expression(RegexMatch.Groups[2].Value, ParentContext).GetValue();
            var name = new Term(RegexMatch.Groups[3].Value);
            var type = new Term(RegexMatch.Groups[1].Value);
            ListValue list;

            if (target is VesselTarget)
            {
                list = Vessel.PartList(type.Text);
            }
            else
            {
                var targetVessel = VesselUtils.GetVesselByName(target.ToString());
                if (targetVessel != null)
                {
                    if (targetVessel.loaded)
                    {
                        list = targetVessel.PartList(type.Text);
                    }
                    else
                    {
                        throw new KOSException("Vessel: " + target + " Is Unloaded, Cannot continue.");
                    }
                }
                else
                {
                    throw new KOSException("Could not get list: " + type.Text + " for Vessel: " + target);
                }
            }

            FindOrCreateVariable(name.Text).Value = list;
            State = ExecutionState.DONE;
        }
    }
}