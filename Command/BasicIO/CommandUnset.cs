using System;
using System.Text.RegularExpressions;
using kOS.Context;

namespace kOS.Command.BasicIO
{
    [Command("UNSET %")]
    public class CommandUnset : Command
    {
        public CommandUnset(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            var varname = RegexMatch.Groups[1].Value;

            if (varname.ToUpper() == "ALL")
            {
                ParentContext.UnsetAll();
            }
            else
            {
                ParentContext.Unset(varname);
            }

            State = ExecutionState.DONE;
        }
    }
}