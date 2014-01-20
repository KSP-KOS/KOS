using System;
using System.Text.RegularExpressions;

namespace kOS.Command.BasicIO
{
    [Command("% OFF")]
    public class CommandSetOff : Command
    {
        public CommandSetOff(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            String varName = RegexMatch.Groups[1].Value;
            Variable v = FindOrCreateVariable(varName);

            if (v == null)
            {
                throw new kOSException("Can't find or create variable '" + varName + "'", this);
            }

            if (!(v.Value is bool) && !(v.Value is float))
            {
                throw new kOSException("That variable can't be set to 'OFF'.", this);
            }

            v.Value = false;
            State = ExecutionState.DONE;
        }
    }
}
