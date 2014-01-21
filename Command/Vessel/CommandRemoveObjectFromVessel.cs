using System.Text.RegularExpressions;
using kOS.Context;
using kOS.Debug;
using kOS.Value;

namespace kOS.Command.Vessel
{
    [Command("REMOVE *")]
    public class CommandRemoveObjectFromVessel : Command
    {
        public CommandRemoveObjectFromVessel(Match regexMatch, IExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            var ex = new Expression.Expression(RegexMatch.Groups[1].Value, this);
            var obj = ex.GetValue();

            var node = obj as Node;
            if (node != null)
            {
                node.Remove();
            }
            else
            {
                throw new KOSException("Supplied object ineligible for removal", this);
            }

            State = ExecutionState.DONE;
        }
    }
}