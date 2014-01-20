using System;
using System.Text.RegularExpressions;

namespace kOS.Command.BasicIO
{
    [Command("% ON")]
    public class CommandSetOn : Command
    {
        public CommandSetOn(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            String varName = RegexMatch.Groups[1].Value;
            Variable v = FindOrCreateVariable(varName);

            if (v != null)
            {
                if (v.Value is bool || v.Value is float)
                {
                    v.Value = true;
                    State = ExecutionState.DONE;
                }
                else
                {
                    throw new kOSException("That variable can't be set to 'ON'.", this);
                }
            }
            else
            {
                throw new kOSException("Can't find or create variable '" + varName + "'", this);
            }
        }
    }
}