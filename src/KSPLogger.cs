using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using kOS;
using kOS.Persistence;
using kOS.Compilation;

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

            string traceText = traceLog();
            LogToScreen(traceText);
            UnityEngine.Debug.Log(traceText);

            // print the call stack
            UnityEngine.Debug.Log(e);
            // print a fragment of the code where the exception ocurred
            List<string> codeFragment = Shared.Cpu.GetCodeFragment(4);
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
        private string traceLog()
        {
            List<int>trace = Shared.Cpu.GetCallTrace();
            string msg = "";
            Opcode thisOpcode;
            Opcode prevOpcode;
            for (int index = 0 ; index < trace.Count ; ++index)
            {
                thisOpcode = Shared.Cpu.GetOpcodeAt(trace[index]);
                
                // The statement "run program" actually causes TWO nested function calls,
                // as the logic to check if the program needs compiling is implemented as a
                // separate kRISC function that gets called from the main code.  Therefore to
                // avoid the same RUN statement giving two nested levels on the call trace,
                // only print the firstmost instance of a contiguous part of the call stack that
                // comes from the same source line:
                if (index > 0)
                {
                    prevOpcode = Shared.Cpu.GetOpcodeAt(trace[index-1]);
                    if (prevOpcode.SourceName == thisOpcode.SourceName &&
                        prevOpcode.SourceLine == thisOpcode.SourceLine)
                    {
                        continue;
                    }
                }
                
                string textLine = getSourceLine(thisOpcode.SourceName, thisOpcode.SourceLine);
                if (msg.Length == 0)
                    msg += "At ";
                else
                    msg += "Called from ";
                
                msg += buildLocationString(thisOpcode.SourceName, thisOpcode.SourceLine) + "\n";
                msg += textLine + "\n";
                int numPadSpaces = thisOpcode.SourceColumn-1;
                if (numPadSpaces < 0)
                    numPadSpaces = 0;
                msg += new String(' ', numPadSpaces) + "^" + "\n";
            }
            return msg;
        }
        
        private string buildLocationString(string source, int line)
        {
            string[] splitParts = source.Split('/');
            
            if (line < 0)
            {
                // Special exception - if line number is negative then this isn't from any
                // line of user's code but from the system itself (like the triggers the compiler builds
                // to recalculate LOCK THROTTLE and LOCK STEERING each time there's an Update).
                return "(kOS built-in Update)";
            }
            else
            {
                if (splitParts.Length > 1)
                {
                    if (source == "interpreter history")
                        return "interpreter line " + line;
                    else
                        return splitParts[1] + " on " + splitParts[0] + ", line " + line;
                }
                else
                    return source + ", line " + line;
            }
        }
        
        private string getSourceLine(string filePath, int line)
        {
            string returnVal = "(Can't show source line)";
            string[] pathParts = filePath.Split('/');
            string fileName = pathParts[ pathParts.Length - 1];
            string volName = "";
            int volNum;
            Volume vol;
            bool getFile = true; // should it try to retrive the file?
            if (line < 0)
            {
                // Special exception - if line number is negative then this isn't from any
                // line of user's code but from the system itself (like the triggers the compiler builds
                // to recalculate LOCK THROTTLE and LOCK STEERING each time there's an Update).
                return "<<System Built-In Flight Control Updater>>";
            }
            if (pathParts.Length > 1)
            {
                volName = pathParts[0];
                if (Regex.IsMatch(volName, @"^\d+$"))
                {
                    // If the volume is a number, then get the volume by integer id.
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
            {
                vol = Shared.VolumeMgr.CurrentVolume;
            }

            if (fileName == "interpreter history")
            {
                // Get the line from the interpreter command history instead of from a file on volume:
                getFile = false;
                returnVal = Shared.Interpreter.GetCommandHistoryAbsolute(line);
            }
            
            if (getFile)
            {
                ProgramFile file = vol.GetByName(fileName);
                if (file!=null)
                {
                    string[] splitLines = file.Content.Split('\n');
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
