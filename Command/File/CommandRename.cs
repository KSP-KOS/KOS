using System;
using System.Text.RegularExpressions;
using kOS.Context;
using kOS.Debug;

namespace kOS.Command.File
{
    [Command("RENAME[VOLUME,FILE]? ^ TO &")]
    public class CommandRename : Command
    {
        public CommandRename(Match regexMatch, IExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            var operation = RegexMatch.Groups[1].Value.Trim();
            var identifier = RegexMatch.Groups[2].Value.Trim();
            var newName = RegexMatch.Groups[3].Value.Trim();

            if (operation.ToUpper() == "VOLUME")
            {
                var targetVolume = GetVolume(identifier); // Will throw if not found

                int intTry;
                if (int.TryParse(newName.Substring(0, 1), out intTry)) throw new KOSException("Volume name cannot start with numeral", this);

                if (targetVolume.Renameable) targetVolume.Name = newName;
                else throw new KOSException("Volume cannot be renamed", this);

                State = ExecutionState.DONE;
                return;
            }

            if (operation.ToUpper() == "FILE" || String.IsNullOrEmpty(operation))
            {
                var f = SelectedVolume.GetByName(identifier);
                if (f == null) throw new KOSException("File '" + identifier + "' not found", this);

                if (SelectedVolume.GetByName(newName) != null)
                {
                    throw new KOSException("File '" + newName + "' already exists.", this);
                }

                int intTry;
                if (int.TryParse(newName.Substring(0, 1), out intTry)) throw new KOSException("Filename cannot start with numeral", this);

                f.Filename = newName;
                State = ExecutionState.DONE;
                return;
            }

            throw new KOSException("Unrecognized renamable object type '" + operation + "'", this);
        }
    }
}