using System.Text.RegularExpressions;

namespace kOS.Command.FlowControl
{
    [Command("BREAK")]
    public class CommandBreak : Command
    {
        public CommandBreak(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            Break();
            State = ExecutionState.DONE;
        }
    }
}
