using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Debug;
using kOS.Persistance;
using kOS.Utilities;

namespace kOS.Context
{
    public class ContextRunProgram : ExecutionContext, IContextRunProgram
    {
        private File file;
        private String commandBuffer;
        private readonly List<Command.Command> commands = new List<Command.Command>();
        private readonly List<Expression.Expression> parameters = new List<Expression.Expression>();
        private const int EXECUTION_LINE = 0;


        public string Filename { get; private set; }

        public ContextRunProgram(IExecutionContext parent, List<Expression.Expression> parameters, String filename) : base(parent) 
        {
            this.parameters = parameters;
            Filename = filename;
        }

        public void Run(File fileObj)
        {
            file = fileObj;

            State = ExecutionState.WAIT;

            RunBlock(fileObj);
        }

        private void RunBlock(IEnumerable<string> block)
        {
            foreach (var line in block.Select(StripComment))
            {
                commandBuffer += line + "\n";
            }

            string cmd;
            var lineNumber = 0;
            int commandLineStart;
            while (ParseNext(ref commandBuffer, out cmd, ref lineNumber, out commandLineStart))
            {
                try
                {
                    Line = commandLineStart;
                    var cmdObj = Command.Command.Get(cmd, this, commandLineStart);
                    commands.Add(cmdObj);
                }
                catch (KOSException e)
                {
                    if (ParentContext.FindClosestParentOfType<IContextRunProgram>() != null)
                    {
                        // Error occurs in a child of another running program
                        StdOut("Error in '" + e.Program.Filename + "' on line " + e.LineNumber + ": " + e.Message);
                        State = ExecutionState.DONE;
                        return;
                    }
                    // Error occurs in the top level program
                    StdOut("Error on line " + e.LineNumber + ": " + e.Message);
                    State = ExecutionState.DONE;
                    return;
                }
                catch (Exception e)
                {
                    // Non-kos exception! This is a bug, but no reason to kill the OS
                    StdOut("Flagrant error on line " + lineNumber);
                    UnityEngine.Debug.Log("Program error");
                    UnityEngine.Debug.Log(e);
                    State = ExecutionState.DONE;
                    return;
                }
            }

            if (commandBuffer.Trim() != "")
            {
                StdOut("End of file reached inside unterminated statement");
                State = ExecutionState.DONE;
            }
        }

        public override bool Break()
        {
            State = ExecutionState.DONE;

            return true;
        }

        public string StripComment(string line)
        {
            for (var i=0; i<line.Length; i++)
            {
                if (line[i] == '\"')
                {
                    i = Utils.FindEndOfString(line, i + 1);
                    if (i == -1) break;
                }
                else if (i < line.Length - 1 && line.Substring(i, 2) == "//")
                {
                    return line.Substring(0, i);
                }
            }

            return line;
        }
        
        public override void Update(float time)
        {
            try
            {
                base.Update(time);
                EvaluateNextCommand();
            }
            catch (KOSException e)
            {
                if (ParentContext.FindClosestParentOfType<IContextRunProgram>() != null)
                {
                    // Error occurs in a child of another running program
                    StdOut("Error in '" + e.Program.Filename + "' on line " + e.LineNumber + ": " + e.Message);
                    State = ExecutionState.DONE;
                }
                else
                {
                    // Error occurs in the top level program
                    StdOut("Error on line " + e.LineNumber + ": " + e.Message);
                    State = ExecutionState.DONE;
                }
            }
            catch (Exception e)
            {
                // Non-kos exception! This is a bug, but no reason to kill the OS
                StdOut("Flagrant error on line " + EXECUTION_LINE);
                UnityEngine.Debug.Log("Program error");
                UnityEngine.Debug.Log(e);
                State = ExecutionState.DONE;
            }
        }

        private void EvaluateNextCommand()
        {
            if (ChildContext != null) return;
            if (commands.Count > 0)
            {
                var cmd = commands[0];
                commands.RemoveAt(0);

                ChildContext = cmd;
                cmd.Evaluate();
            }
            else
            {
                State = ExecutionState.DONE;
            }
        }

        public object PopParameter()
        {
            if (parameters.Count > 0)
            {
                var retValue = parameters[0].GetValue();
                parameters.RemoveAt(0);

                return retValue;
            }

            throw new KOSException("Wrong number of parameters supplied");
        }
    }
}
