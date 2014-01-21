using System.Text.RegularExpressions;
using kOS.Context;

namespace kOS.Command.BasicIO
{
    [Command("CALL *")]
    public class CommandCallExternal : Command
    {
        public CommandCallExternal(Match regexMatch, IExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            // External functions are now handled within expressions,
            // so simply execute the expression and throw away the value
            var subEx = new Expression.Expression(RegexMatch.Groups[1].Value, this);
            subEx.GetValue();
            
            State = ExecutionState.DONE;
        }
    }
}
