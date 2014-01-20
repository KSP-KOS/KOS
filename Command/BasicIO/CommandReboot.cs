using System.Text.RegularExpressions;

namespace kOS.Command.BasicIO
{
    [Command("REBOOT")]
    public class CommandReboot : Command
    {
        public CommandReboot(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            ParentContext.SendMessage(SystemMessage.RESTART);
            State = ExecutionState.DONE;
        }
    }
}