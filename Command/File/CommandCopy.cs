using System.Text.RegularExpressions;
using kOS.Context;
using kOS.Debug;

namespace kOS.Command.File
{
    [Command("COPY &[TO,FROM][VOLUME]? ^")]
    public class CommandCopy : Command
    {
        public CommandCopy(Match regexMatch, IExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            var targetFile = RegexMatch.Groups[1].Value.Trim();
            var volumeName = RegexMatch.Groups[4].Value.Trim();
            var operation = RegexMatch.Groups[2].Value.Trim().ToUpper();

            var targetVolume = GetVolume(volumeName); // Will throw if not found

            Persistance.File file;

            switch (operation)
            {
                case "FROM":
                    file = targetVolume.GetByName(targetFile);
                    if (file == null) throw new KOSException("File '" + targetFile + "' not found", this);
                    if (!SelectedVolume.SaveFile(new Persistance.File(file))) throw new KOSException("File copy failed", this);
                    break;

                case "TO":
                    file = SelectedVolume.GetByName(targetFile);
                    if (file == null) throw new KOSException("File '" + targetFile + "' not found", this);
                    if (!targetVolume.SaveFile(new Persistance.File(file))) throw new KOSException("File copy failed", this);
                    break;
            }

            State = ExecutionState.DONE;
        }
    }
}