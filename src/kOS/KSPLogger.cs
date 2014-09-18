using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using kOS.Persistence;
using kOS.Safe.Compilation;

namespace kOS
{
    public class KSPLogger : Logger
    {
        public KSPLogger(SharedObjects shared) : base(shared)
        {
        }

        public override void Log(string text)
        {
            base.Log(text);
            UnityEngine.Debug.Log(text);
        }

        public override void Log(Exception e)
        {
            base.Log(e);

            string traceText = TraceLog();
            LogToScreen(traceText);
            UnityEngine.Debug.Log(traceText);

            // print the call stack
            UnityEngine.Debug.Log(e);
            // print a fragment of the code where the exception ocurred
            List<string> codeFragment = Shared.Cpu.GetCodeFragment(16);
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine("Code Fragment");
            foreach (string instruction in codeFragment)
                messageBuilder.AppendLine(instruction);
            UnityEngine.Debug.Log(messageBuilder.ToString());
        }
        
        /// <summary>
        /// Return a list of strings containing the trace log of the call stack that got to
        /// the current point.
        /// </summary>
        /// <returns></returns>
        private string TraceLog()
        {
            List<int>trace = Shared.Cpu.GetCallTrace();
            string msg = "";
            for (int index = 0 ; index < trace.Count ; ++index)
            {
                Opcode thisOpcode = Shared.Cpu.GetOpcodeAt(trace[index]);
                if (thisOpcode is OpcodeBogus) 
                {
                    return "(Cannot Show kOS Error Location - error might really be internal. See kOS devs.)";
                }
                
                // The statement "run program" actually causes TWO nested function calls,
                // as the logic to check if the program needs compiling is implemented as a
                // separate kRISC function that gets called from the main code.  Therefore to
                // avoid the same RUN statement giving two nested levels on the call trace,
                // only print the firstmost instance of a contiguous part of the call stack that
                // comes from the same source line:
                if (index > 0)
                {
                    Opcode prevOpcode = Shared.Cpu.GetOpcodeAt(trace[index-1]);
                    if (prevOpcode.SourceName == thisOpcode.SourceName &&
                        prevOpcode.SourceLine == thisOpcode.SourceLine)
                    {
                        continue;
                    }
                }

                string textLine = GetSourceLine(thisOpcode.SourceName, thisOpcode.SourceLine);
                if (msg.Length == 0)
                    msg += "At ";
                else
                    msg += "Called from ";
                
                msg += BuildLocationString(thisOpcode.SourceName, thisOpcode.SourceLine) + "\n";
                msg += textLine + "\n";
                if (thisOpcode.SourceColumn > 0)
                {
                    int numPadSpaces = thisOpcode.SourceColumn-1;
                    if (numPadSpaces < 0)
                        numPadSpaces = 0;
                    msg += new String(' ', numPadSpaces) + "^" + "\n";
                }
            }
            return msg;
        }
        
        private string BuildLocationString(string source, int line)
        {
            string[] splitParts = source.Split('/');
            
            if (line < 0)
            {
                // Special exception - if line number is negative then this isn't from any
                // line of user's code but from the system itself (like the triggers the compiler builds
                // to recalculate LOCK THROTTLE and LOCK STEERING each time there's an Update).
                return "(kOS built-in Update)";
            }

            if (splitParts.Length <= 1)
                return string.Format("{0}, line {1}", source, line);
            if (source == "interpreter history")
                return string.Format("interpreter line {0}", line);
            return string.Format("{0} on {1}, line {2}", splitParts[1], splitParts[0], line);
        }
        
        private string GetSourceLine(string filePath, int line)
        {
            string returnVal = "(Can't show source line)";
            string[] pathParts = filePath.Split('/');
            string fileName = pathParts.Last();
            Volume vol;
            if (line < 0 && (filePath==null || filePath == String.Empty))
            {
                // Special exception - if line number is negative then this isn't from any
                // line of user's code but from the system itself (like the triggers the compiler builds
                // to recalculate LOCK THROTTLE and LOCK STEERING each time there's an Update).
                return "<<System Built-In Flight Control Updater>>";
            }
            if (pathParts!=null && pathParts.Length > 1)
            {
                string volName = pathParts.First();
                if (Regex.IsMatch(volName, @"^\d+$"))
                {
                    // If the volume is a number, then get the volume by integer id.
                    int volNum;
                    int.TryParse(volName, out volNum);
                    vol = Shared.VolumeMgr.GetVolume(volNum);
                }
                else
                {
                    // If the volume is not a number, then get the volume by name string.
                    vol = Shared.VolumeMgr.GetVolume(volName);
                }
            }
            else
                vol = Shared.VolumeMgr.CurrentVolume;
            
            if (fileName == "interpreter history")
                return Shared.Interpreter.GetCommandHistoryAbsolute(line);
            
            ProgramFile file = vol.GetByName(fileName);
            if (file!=null)
            {
                if (file.Category == FileCategory.KEXE)
                    return  "<<machine language file: can't show source line>>";
                else
                {
                    string[] splitLines = file.StringContent.Split('\n');
                    if (splitLines.Length >= line)
                    {
                        returnVal = splitLines[line-1];
                    }
                }
            }
            return returnVal;
        }

    }
}
