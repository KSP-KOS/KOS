using System.Text.RegularExpressions;

namespace kOS.Command
{
    [Command("CALL *")]
    public class CommandCallExternal : Command
    {
        public CommandCallExternal(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            // External functions are now handled within expressions,
            // so simply execute the expression and throw away the value
            Expression subEx = new Expression(RegexMatch.Groups[1].Value, this);
            subEx.GetValue();
            
            State = ExecutionState.DONE;
        }
    }
}
