using System.Text.RegularExpressions;
using kOS.Debug;

namespace kOS.Command.Vessel
{
    [Command("REMOVE *")]
    public class CommandRemoveObjectFromVessel : Command
    {
        public CommandRemoveObjectFromVessel(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            var ex = new Expression(RegexMatch.Groups[1].Value, this);
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