using System.Text.RegularExpressions;
using kOS.Context;

namespace kOS.Command.BasicIO
{
    [Command("REBOOT")]
    public class CommandReboot : Command
    {
        public CommandReboot(Match regexMatch, IExecutionContext context) : base(regexMatch, context)
        {
        }

        public override void Evaluate()
        {
            ParentContext.SendMessage(SystemMessage.RESTART);
            State = ExecutionState.DONE;
        }
    }
}