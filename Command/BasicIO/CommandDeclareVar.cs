using System;
using System.Text.RegularExpressions;
using kOS.Binding;
using kOS.Context;
using kOS.Debug;

namespace kOS.Command.BasicIO
{
    [Command("DECLARE %")]
    public class CommandDeclareVar : Command
    {
        public CommandDeclareVar(Match regexMatch, IExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            String varName = RegexMatch.Groups[1].Value;
            Variable v = FindOrCreateVariable(varName);

            if (v == null) throw new KOSException("Can't create variable '" + varName + "'", this);
            
            State = ExecutionState.DONE;
        }
    }
}