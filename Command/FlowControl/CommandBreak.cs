using System.Text.RegularExpressions;
using kOS.Context;

namespace kOS.Command.FlowControl
{
    [Command("BREAK")]
    public class CommandBreak : Command
    {
        public CommandBreak(Match regexMatch, IExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            Break();
            State = ExecutionState.DONE;
        }
    }
}
