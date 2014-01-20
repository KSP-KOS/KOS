using System.Text.RegularExpressions;

namespace kOS.Command.BasicIO
{
    [Command("SHUTDOWN")]
    public class CommandShutdown : Command
    {
        public CommandShutdown(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            ParentContext.SendMessage(SystemMessage.SHUTDOWN);
            State = ExecutionState.DONE;
        }
    }
}