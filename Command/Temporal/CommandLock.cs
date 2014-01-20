using System.Text.RegularExpressions;

namespace kOS.Command
{
    [Command("LOCK % TO *")]
    public class CommandLock : Command
    {
        public CommandLock(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            string varname = RegexMatch.Groups[1].Value;
            Expression expression = new Expression(RegexMatch.Groups[2].Value, ParentContext);

            ParentContext.Unlock(varname);
            ParentContext.Lock(varname, expression);

            State = ExecutionState.DONE;
        }
    }
}