using System;
using System.Text.RegularExpressions;

namespace kOS.Command.BasicIO
{
    [Command("DECLARE %")]
    public class CommandDeclareVar : Command
    {
        public CommandDeclareVar(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            String varName = RegexMatch.Groups[1].Value;
            Variable v = FindOrCreateVariable(varName);

            if (v == null) throw new kOSException("Can't create variable '" + varName + "'", this);
            
            State = ExecutionState.DONE;
        }
    }
}