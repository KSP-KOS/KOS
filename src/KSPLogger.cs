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
            Opcode nextOpcode;
            for (int index = 0 ; index < trace.Count ; ++index)
            {
                thisOpcode = Shared.Cpu.GetOpcodeAt(trace[index]);
                
                // The statement "run program" actually causes TWO nested function calls,
                // as the logic to check if the program needs compiling is implemented as a
                // separate kRISC function that gets called from the main code.  Therefore to
                // avoid the same RUN statement giving two nested levels on the call trace,
                // only print the lastmost instance of a contiguous part of the call stack that
                // comes from the same source line:
                if (index+1 < trace.Count)
                {
                    nextOpcode = Shared.Cpu.GetOpcodeAt(trace[index+1]);
                    if (nextOpcode.SourceName == thisOpcode.SourceName &&
                        nextOpcode.SourceLine == thisOpcode.SourceLine)
                    {
                        continue;
                    }
                }
                
                string textLine = getSourceLine(thisOpcode.SourceName, thisOpcode.SourceLine);
                if (msg.Length == 0)
                    msg += "At ";
                else
                    msg += "Called from ";
                
                msg += thisOpcode.SourceName + ", line: " + thisOpcode.SourceLine + "\n";
                msg += textLine + "\n";
                msg += new String(' ',thisOpcode.SourceColumn-1) + "^" + "\n";
            }
            return msg;
        }
        
        private string getSourceLine(string filePath, int line)
        {
            string returnVal = "(Can't show source line)";
            string[] pathParts = filePath.Split('/');
            string fileName = pathParts[ pathParts.Length - 1];
            string volName = "";
            int volNum;
            Volume vol;
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
            
            ProgramFile file = vol.GetByName(fileName);
            
            if (file!=null)
            {
                // works on both unix or windows EOLN's, but leaves an extra '\r' in the string for windows, which is not fatal.
                string[] splitLines = file.Content.Split('\n');
                if (splitLines.Length >= line)
                {
                    returnVal = splitLines[line-1];
                }
            }
            
            return returnVal;
        }
        
    }
}
