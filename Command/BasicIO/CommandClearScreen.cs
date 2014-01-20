using System.Text.RegularExpressions;

namespace kOS.Command.BasicIO
{
    [Command("CLEARSCREEN")]
    public class CommandClearScreen : Command
    {
        public CommandClearScreen(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            ParentContext.SendMessage(SystemMessage.CLEARSCREEN);
            State = ExecutionState.DONE;
        }
    }
}