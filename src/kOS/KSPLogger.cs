using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using kOS.Safe.Compilation;
using kOS.Safe.Persistence;
using kOS.Safe.Encapsulation;
using kOS.Screen;
using kOS.Safe.Exceptions;
using kOS.Safe.Execution;

namespace kOS
{
    public class KSPLogger : Logger
    {
        public const string LOGGER_PREFIX = "kOS:";
        public KSPLogger(SharedObjects shared) : base(shared)
        {
        }

        public KSPLogger()
        {
            
        }

        public override void Log(string text)
        {
            UnityEngine.Debug.Log(string.Format("{0} {1}", LOGGER_PREFIX, text));
        }

        public override void LogWarning(string s)
        {
            UnityEngine.Debug.LogWarning(string.Format("{0} {1}", LOGGER_PREFIX, s));
        }
        
        public override void LogWarningAndScreen(string s)
        {
            LogWarning(s);
            LogToScreen(s);
            ScreenMessages.PostScreenMessage("<color=#dddd55><size=30>" + s + "</size></color>", 20, ScreenMessageStyle.UPPER_CENTER);
        }

        public override void LogException(Exception exception)
        {
            UnityEngine.Debug.LogException(exception);
        }

        public override void LogError(string s)
        {
            UnityEngine.Debug.LogError(string.Format("{0} {1}", LOGGER_PREFIX, s));
        }

        public override void Log(Exception e)
        {
            base.Log(e);

            string traceText = TraceLog();
            LogToScreen(traceText);
            var kosText = string.Format("{0} {1}", LOGGER_PREFIX, traceText);
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
            
            if (Shared != null && Shared.Cpu != null)
            {
                // print a fragment of the code where the exception ocurred
                int logContextLines = 16;
#if DEBUG
                logContextLines = 999999; // in debug mode let's just dump everything because it's easier that way.
#endif
                List<string> codeFragment = Shared.Cpu.GetCodeFragment(logContextLines);
                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine("Code Fragment");
                foreach (string instruction in codeFragment)
                    messageBuilder.AppendLine(instruction);
                UnityEngine.Debug.Log(messageBuilder.ToString());
            }
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
                    // skip the level of the stack trace that passes through the boilerplate
                    // load runner code:
                    if (index > 0)
                    {
                        if (thisOpcode.SourcePath == null || thisOpcode.SourcePath.VolumeId.Equals(ProgramBuilder.BuiltInFakeVolumeId))
                        {
                            continue;
                        }
                    }

                    string textLine = (thisOpcode is OpcodeEOF) ? "<<--EOF" : GetSourceLine(thisOpcode.SourcePath, thisOpcode.SourceLine);
                    
                    if (msg.Length == 0)
                        msg += "At ";
                    else
                        msg += "Called from ";
                    
                    msg += (thisOpcode is OpcodeEOF) ? Terminal.InterpreterName
                        : BuildLocationString(thisOpcode.SourcePath, thisOpcode.SourceLine);
                    msg += "\n" + textLine + "\n";

                    int useColumn = (thisOpcode is OpcodeEOF) ? 1 : thisOpcode.SourceColumn;
                    if (useColumn > 0)
                    {
                        int numPadSpaces = useColumn-1;
                        if (numPadSpaces < 0)
                            numPadSpaces = 0;
                        msg += new string(' ', numPadSpaces) + "^" + "\n";
                    }
                }
                return msg;
            }
            catch (Exception ex) //INTENTIONAL POKEMON
            {
                UnityEngine.Debug.Log(string.Format("{0} Logger: {1}", LOGGER_PREFIX, ex.Message));
                UnityEngine.Debug.Log(string.Format("{0} Logger: {1}", LOGGER_PREFIX, ex.StackTrace));
                return BOGUS_MESSAGE;
            }
        }
        
        private string BuildLocationString(GlobalPath path, int line)
        {
            if (line < 0)
            {
                // Special exception - if line number is negative then this isn't from any
                // line of user's code but from the system itself (like the triggers the compiler builds
                // to recalculate LOCK THROTTLE and LOCK STEERING each time there's an Update).
                return "(kOS built-in Update)";
            }

            return string.Format("{0}, line {1}", path, line);
        }
        
        private string GetSourceLine(GlobalPath path, int line)
        {
            string returnVal = "(Can't show source line)";

            if (line < 0)
            {
                // Special exception - if line number is negative then this isn't from any
                // line of user's code but from the system itself (like the triggers the compiler builds
                // to recalculate LOCK THROTTLE and LOCK STEERING each time there's an Update).
                return "<<System Built-In Flight Control Updater>>";
            }

            if (path is InternalPath)
            {
                return (path as InternalPath).Line(line);
            }

            Volume vol;

            try
            {
                vol = Shared.VolumeMgr.GetVolumeFromPath(path);
            }
            catch (KOSPersistenceException)
            {
                return returnVal;
            }

            VolumeFile file = vol.Open(path) as VolumeFile;
            if (file != null)
            {
                if (file.ReadAll().Category == FileCategory.KSM)
                    return  "<<machine language file: can't show source line>>";

                string[] splitLines = file.ReadAll().String.Split('\n');
                if (splitLines.Length >= line)
                {
                    returnVal = splitLines[line-1];
                }
            }

            return returnVal;
        }
    }
}
