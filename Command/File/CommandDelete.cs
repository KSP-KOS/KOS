using System;
using System.Text.RegularExpressions;
using kOS.Debug;

namespace kOS.Command.File
{
    [Command("DELETE &[FROM,FROM VOLUME]?[:^]?")]
    public class CommandDelete : Command
    {
        public CommandDelete(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            var targetFile = RegexMatch.Groups[1].Value.Trim();
            var volumeName = RegexMatch.Groups[3].Value.Trim();

            kOS.File file;

            if (volumeName.Trim() != "")
            {
                var targetVolume = GetVolume(volumeName);
                file = targetVolume.GetByName(targetFile);
                if (file == null) throw new KOSException("File '" + targetFile + "' not found", this);
                targetVolume.DeleteByName(targetFile);
            }
            else
            {
                file = SelectedVolume.GetByName(targetFile);
                if (file == null) throw new KOSException("File '" + targetFile + "' not found", this);
                SelectedVolume.DeleteByName(targetFile);
            }

            State = ExecutionState.DONE;
        }
    }
}