using System.Text.RegularExpressions;
using kOS.Context;

namespace kOS.Command.File
{
    [Command("LOG * TO &")]
    public class CommandLog: Command
    {
        public CommandLog(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            // Todo: let the user specify a volume "LOG something TO file ON volume"
            var targetVolume = SelectedVolume;

            // If the archive is out of reach, the signal is lost in space.
            if (!targetVolume.CheckRange())
            {
                State = ExecutionState.DONE;
                return;
            }

            var targetFile = RegexMatch.Groups[2].Value.Trim();
            var e = new Expression.Expression(RegexMatch.Groups[1].Value, ParentContext);

            if (e.IsNull())
            {
                State = ExecutionState.DONE;
            }
            else
            {
                targetVolume.AppendToFile(targetFile, e.ToString());
                State = ExecutionState.DONE;
            }
        }
    }
}