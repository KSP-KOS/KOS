using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace kOS
{
    [CommandAttribute("EDIT &")]
    public class CommandEditFile : Command
    {
        public CommandEditFile(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            String fileName = RegexMatch.Groups[1].Value;

            if (ParentContext is ImmediateMode)
            {
                ParentContext.Push(new InterpreterEdit(fileName, ParentContext));
            }
            else
            {
                throw new kOSException("Edit can only be used when in immediate mode.");
            }
        }
    }

    [CommandAttribute(@"^RUN ([a-zA-Z0-9\-_]+?)( ?\((.*?)\))?$")]
    public class CommandRunFile : Command
    {
        public CommandRunFile(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }
          
        public override void Evaluate()
        {
            String fileName = RegexMatch.Groups[1].Value;
            File file = SelectedVolume.GetByName(fileName);
            var parameters = new List<Expression>();

            if (RegexMatch.Groups.Count > 1)
            {
                String paramString = RegexMatch.Groups[3].Value;
                foreach (String param in Utils.ProcessParams(paramString))
                {
                    Expression subEx = new Expression(param, this);
                    parameters.Add(subEx);
                }
            }

            if (file != null)
            {
                ContextRunProgram runContext = new ContextRunProgram(this, parameters);
                Push(runContext);

                if (file.Count > 0)
                {
                    runContext.Run(file);
                    State = ExecutionState.WAIT;
                }
                else
                {
                    State = ExecutionState.DONE;
                }
            }
            else
            {
                throw new kOSException("File not found '" + fileName + "'.");
            }
        }

        public override bool Type(char c)
        {
            if (State == ExecutionState.WAIT)
            {
                return true;
            }
            else
            {
                return base.Type(c);
            }
        }

        public override bool SpecialKey(kOSKeys key)
        {
            if (key == kOSKeys.BREAK)
            {
                StdOut("Program aborted.");
                State = ExecutionState.DONE;

                // Bypass child contexts
                return true;
            }

            return base.SpecialKey(key);
        }

        public override void Update(float time)
        {
            try
            {
                base.Update(time);
            }
            catch (kOSException e)
            {
                StdOut("Error: " + e.Message);
                StdOut("Program aborted.");
                State = ExecutionState.DONE;
            }

            if (ChildContext == null)
            {
                State = ExecutionState.DONE;
            }
            else if (ChildContext.State == ExecutionState.DONE)
            {
                if (ParentContext is ImmediateMode)
                {
                    StdOut("Program ended.");
                }

                State = ExecutionState.DONE;
            }
        }
    }
    
    [CommandAttribute("SWITCH TO ^")]
    public class CommandSwitch : Command
    {
        public CommandSwitch(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            String targetVolume = RegexMatch.Groups[1].Value.Trim();
            int volID;

            if (int.TryParse(targetVolume, out volID))
            {
                if (!ParentContext.SwitchToVolume(volID))
                {
                    throw new kOSException("Volume " + volID + " not found");
                }
            }
            else
            {
                if (!ParentContext.SwitchToVolume(targetVolume))
                {
                    throw new kOSException("Volume '" + targetVolume + "' not found");
                }
            }

            State = ExecutionState.DONE;
        }
    }
    
    [CommandAttribute("RENAME[VOLUME,FILE]? ^ TO &")]
    public class CommandRename : Command
    {
        public CommandRename(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            String operation = RegexMatch.Groups[1].Value.Trim();
            String identifier = RegexMatch.Groups[2].Value.Trim();
            String newName = RegexMatch.Groups[3].Value.Trim();

            if (operation.ToUpper() == "VOLUME")
            {
                Volume targetVolume = GetVolume(identifier); // Will throw if not found

                int intTry;
                if (int.TryParse(newName.Substring(0, 1), out intTry)) throw new kOSException("Volume name cannot start with numeral");

                if (targetVolume.Renameable) targetVolume.Name = newName;
                else throw new kOSException("Volume cannot be renamed");

                State = ExecutionState.DONE;
                return;
            }
            else if (operation.ToUpper() == "FILE" || String.IsNullOrEmpty(operation))
            {
                File f = SelectedVolume.GetByName(identifier);
                if (f == null) throw new kOSException("File '" + identifier + "' not found");

                if (SelectedVolume.GetByName(newName) != null)
                {
                    throw new kOSException("File '" + newName + "' already exists.");
                }

                int intTry;
                if (int.TryParse(newName.Substring(0, 1), out intTry)) throw new kOSException("Filename cannot start with numeral");

                f.Filename = newName;
                State = ExecutionState.DONE;
                return;
            }

            throw new kOSException("Unrecognized renamable object type '" + operation + "'");
        }
    }

    [CommandAttribute("LOG * TO &")]
    public class CommandLog: Command
    {
        public CommandLog(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            // Todo: let the user specify a volume "LOG something TO file ON volume"
            Volume targetVolume = SelectedVolume;

            // If the archive is out of reach, the signal is lost in space.
            if (!targetVolume.CheckRange())
            {
                State = ExecutionState.DONE;
                return;
            }

            String targetFile = RegexMatch.Groups[2].Value.Trim();
            Expression e = new Expression(RegexMatch.Groups[1].Value, ParentContext);

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
    
    [CommandAttribute("COPY &[TO,FROM][VOLUME]? ^")]
    public class CommandCopy : Command
    {
        public CommandCopy(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            String targetFile = RegexMatch.Groups[1].Value.Trim();
            String volumeName = RegexMatch.Groups[4].Value.Trim();
            String operation = RegexMatch.Groups[2].Value.Trim().ToUpper();

            Volume targetVolume = GetVolume(volumeName); // Will throw if not found

            File file = null;

            switch (operation)
            {
                case "FROM":
                    file = targetVolume.GetByName(targetFile);
                    if (file == null) throw new kOSException("File '" + targetFile + "' not found");
                    if (!SelectedVolume.SaveFile(new File(file))) throw new kOSException("File copy failed");
                    break;

                case "TO":
                    file = SelectedVolume.GetByName(targetFile);
                    if (file == null) throw new kOSException("File '" + targetFile + "' not found");
                    if (!targetVolume.SaveFile(new File(file))) throw new kOSException("File copy failed");
                    break;
            }

            State = ExecutionState.DONE;
        }
    }

    [CommandAttribute("DELETE &[FROM,FROM VOLUME]?[:^]?")]
    public class CommandDelete : Command
    {
        public CommandDelete(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            String targetFile = RegexMatch.Groups[1].Value.Trim();
            String volumeName = RegexMatch.Groups[3].Value.Trim();

            File file = null;
            Volume targetVolume = null;

            if (volumeName.Trim() != "")
            {
                targetVolume = GetVolume(volumeName); // Will throw if not found
                file = targetVolume.GetByName(targetFile);
                if (file == null) throw new kOSException("File '" + targetFile + "' not found");
                targetVolume.DeleteByName(targetFile);
            }
            else
            {
                file = SelectedVolume.GetByName(targetFile);
                if (file == null) throw new kOSException("File '" + targetFile + "' not found");
                SelectedVolume.DeleteByName(targetFile);
            }

            State = ExecutionState.DONE;
        }
    }
    
    [CommandAttribute("LIST[VOLUMES,FILES]?")]
    public class CommandList : Command
    {
        public CommandList(Match regexMatch, ExecutionContext context) : base(regexMatch,  context) { }

        public override void Evaluate()
        {
            String listType = RegexMatch.Groups[1].Value.Trim().ToUpper();

            if (listType == "FILES" || String.IsNullOrEmpty(listType))
            {
                StdOut("");

                StdOut("Volume " + GetVolumeBestIdentifier(SelectedVolume));
                StdOut("-------------------------------------");                

                foreach (FileInfo fileInfo in SelectedVolume.GetFileList())
                {
                    StdOut(fileInfo.Name.PadRight(30, ' ') + fileInfo.Size.ToString());
                }
                
                int freeSpace = SelectedVolume.GetFreeSpace();
                StdOut("Free space remaining: " + (freeSpace > -1 ? freeSpace.ToString() : " infinite"));

                StdOut("");

                State = ExecutionState.DONE;
                return;
            }
            else if (listType == "VOLUMES")
            {
                StdOut("");
                StdOut("ID    Name                    Size");
                StdOut("-------------------------------------");

                int i = 0;

                foreach (Volume volume in Volumes)
                {
                    String id = i.ToString();
                    if (volume == SelectedVolume) id = "*" + id;

                    String line = id.PadLeft(2).PadRight(6, ' ');
                    line += volume.Name.PadRight(24, ' ');

                    String size = volume.CheckRange() ? (volume.Capacity > -1 ? volume.Capacity.ToString() : "Inf") : "Disc";
                    line += size;

                    StdOut(line);

                    i++;
                }

                StdOut("");

                State = ExecutionState.DONE;
                return;
            }

            throw new kOSException("List type '" + listType + "' not recognized.");
        }
    }
}
