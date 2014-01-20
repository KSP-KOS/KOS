using System;
using System.Globalization;
using System.Text.RegularExpressions;
using kOS.Context;
using kOS.Debug;
using kOS.Persistance;

namespace kOS.Command.File
{
    [Command("LIST[VOLUMES,FILES]?")]
    public class CommandList : Command
    {
        public CommandList(Match regexMatch, ExecutionContext context) : base(regexMatch,  context) { }

        public override void Evaluate()
        {
            var listType = RegexMatch.Groups[1].Value.Trim().ToUpper();

            if (listType == "FILES" || String.IsNullOrEmpty(listType))
            {
                StdOut("");

                StdOut("Volume " + GetVolumeBestIdentifier(SelectedVolume));
                StdOut("-------------------------------------");                

                foreach (var fileInfo in SelectedVolume.GetFileList())
                {
                    StdOut(fileInfo.Name.PadRight(30, ' ') + fileInfo.Size.ToString(CultureInfo.InvariantCulture));
                }
                
                int freeSpace = SelectedVolume.GetFreeSpace();
                StdOut("Free space remaining: " + (freeSpace > -1 ? freeSpace.ToString(CultureInfo.InvariantCulture) : " infinite"));

                StdOut("");

                State = ExecutionState.DONE;
                return;
            }

            if (listType == "VOLUMES")
            {
                StdOut("");
                StdOut("ID    Name                    Size");
                StdOut("-------------------------------------");

                int i = 0;

                foreach (Volume volume in Volumes)
                {
                    String id = i.ToString(CultureInfo.InvariantCulture);
                    if (volume == SelectedVolume) id = "*" + id;

                    String line = id.PadLeft(2).PadRight(6, ' ');
                    line += volume.Name.PadRight(24, ' ');

                    String size = volume.CheckRange() ? (volume.Capacity > -1 ? volume.Capacity.ToString(CultureInfo.InvariantCulture) : "Inf") : "Disc";
                    line += size;

                    StdOut(line);

                    i++;
                }

                StdOut("");

                State = ExecutionState.DONE;
                return;
            }

            throw new KOSException("List type '" + listType + "' not recognized.", this);
        }
    }
}
