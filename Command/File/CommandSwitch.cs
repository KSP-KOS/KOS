using System.Text.RegularExpressions;

namespace kOS.Command.File
{
    [Command("SWITCH TO ^")]
    public class CommandSwitch : Command
    {
        public CommandSwitch(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            var targetVolume = RegexMatch.Groups[1].Value.Trim();
            int volID;

            if (int.TryParse(targetVolume, out volID))
            {
                if (!ParentContext.SwitchToVolume(volID))
                {
                    throw new kOSException("Volume " + volID + " not found", this);
                }
            }
            else
            {
                if (!ParentContext.SwitchToVolume(targetVolume))
                {
                    throw new kOSException("Volume '" + targetVolume + "' not found", this);
                }
            }

            State = ExecutionState.DONE;
        }
    }
}