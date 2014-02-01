using System.Text.RegularExpressions;
using kOS.Context;
using kOS.Debug;

namespace kOS.Command.BasicIO
{
    [Command("% OFF")]
    public class CommandSetOff : Command
    {
        public CommandSetOff(Match regexMatch, IExecutionContext context) : base(regexMatch, context)
        {
        }

        public override void Evaluate()
        {
            var varName = RegexMatch.Groups[1].Value;
            var v = FindOrCreateVariable(varName);

            if (v == null)
            {
                throw new KOSException("Can't find or create variable '" + varName + "'", this);
            }

            if (!(v.Value is bool) && !(v.Value is float))
            {
                throw new KOSException("That variable can't be set to 'OFF'.", this);
            }

            v.Value = false;
            State = ExecutionState.DONE;
        }
    }
}