using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using kOS.Safe.Compilation;
using kOS.Safe.Persistence;

namespace kOS
{
    public class KSPLogger : Logger
    {
        public KSPLogger(SharedObjects shared) : base(shared)
        {
        }

        public KSPLogger()
        {
            
        }

        public override void Log(string text)
        {
            base.Log(text);
            UnityEngine.Debug.Log(string.Format("kOS: {0}", text));
        }

        public override void LogWarning(string s)
        {
            UnityEngine.Debug.LogWarning(string.Format("kOS: {0}", s));
        }

        public override void LogException(Exception exception)
        {
            UnityEngine.Debug.LogException(exception);
        }

        public override void LogError(string s)
        {
            UnityEngine.Debug.LogError(string.Format("kOS: {0}", s));
        }

        public override void Log(Exception e)
        {
            base.Log(e);

            string traceText = TraceLog();
            LogToScreen(traceText);
            var kosText = string.Format("kOS: {0}", traceText);
            UnityEngine.Debug.Log(kosText);
            
            // -------------
            //    TODO
            // -------------
            // KOSExceptions probably should contain a reference to the stackTrace
            // information that TraceLog() builds up, and then in here when the
            // stack trace gets calculated by TraceLog(), it should also get assigned
            // to the exception object e's stackTrace reference.  That way when
            // we have a list storing the exception history, the exceptions can contain
            // their kRISC tracelogs to pore through.

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
            const string BOGUS_MESSAGE = "(Cannot Show kOS Error Location - error might really be internal. See kOS devs.)";
            try
            {
                List<int> trace = Shared.Cpu.GetCallTrace();
                string msg = "";
                for (int index = 0 ; index < trace.Count ; ++index)
                {
                    Opcode thisOpcode = Shared.Cpu.GetOpcodeAt(trace[index]);
                    if (thisOpcode is OpcodeBogus)
                    {
                        return BOGUS_MESSAGE;
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

                    string textLine = (thisOpcode is OpcodeEOF) ? "<<--EOF" : GetSourceLine(thisOpcode.SourceName, thisOpcode.SourceLine);
                    
                    if (msg.Length == 0)
                        msg += "At ";
                    else
                        msg += "Called from ";
                    
                    msg += (thisOpcode is OpcodeEOF) ? "interpreter" : BuildLocationString(thisOpcode.SourceName, thisOpcode.SourceLine);
                    msg += "\n" + textLine + "\n";

                    int useColumn = (thisOpcode is OpcodeEOF) ? 1 : thisOpcode.SourceColumn;
                    if (useColumn > 0)
                    {
                        int numPadSpaces = useColumn-1;
                        if (numPadSpaces < 0)
                            numPadSpaces = 0;
                        msg += new String(' ', numPadSpaces) + "^" + "\n";
                    }
                }
                return msg;
            }
            catch (Exception ex) //INTENTIONAL POKEMON
            {
                UnityEngine.Debug.Log("kOS: Logger: " + ex.Message);
                UnityEngine.Debug.Log("kOS: Logger: " + ex.StackTrace);
                return BOGUS_MESSAGE;
            }
        }
        
        private string BuildLocationString(string source, int line)
        {
            if (line < 0)
            {
                // Special exception - if line number is negative then this isn't from any
                // line of user's code but from the system itself (like the triggers the compiler builds
                // to recalculate LOCK THROTTLE and LOCK STEERING each time there's an Update).
                return "(kOS built-in Update)";
            }
            if (string.IsNullOrEmpty(source))
            {
                return "<<probably internal kOS C# error>>";
            }

            string[] splitParts = source.Split('/');

            if (splitParts.Length <= 1)
                return string.Format("{0}, line {1}", source, line);
            if (source == "interpreter history")
                return string.Format("interpreter line {0}", line);
            return string.Format("{0} on {1}, line {2}", splitParts[1], splitParts[0], line);
        }
        
        private string GetSourceLine(string filePath, int line)
        {
            string returnVal = "(Can't show source line)";
            if (line < 0 && string.IsNullOrEmpty(filePath))
            {
                // Special exception - if line number is negative then this isn't from any
                // line of user's code but from the system itself (like the triggers the compiler builds
                // to recalculate LOCK THROTTLE and LOCK STEERING each time there's an Update).
                return "<<System Built-In Flight Control Updater>>";
            }

            if (string.IsNullOrEmpty(filePath))
            {
                return "<<Probably internal error within kOS C# code>>";
            }
            string[] pathParts = filePath.Split('/');
            string fileName = pathParts.Last();
            Volume vol;
            if (pathParts.Length > 1)
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
                if (file.Category == FileCategory.KSM)
                    return  "<<machine language file: can't show source line>>";

                string[] splitLines = file.StringContent.Split('\n');
                if (splitLines.Length >= line)
                {
                    returnVal = splitLines[line-1];
                }
            }
            return returnVal;
        }

    }
}
