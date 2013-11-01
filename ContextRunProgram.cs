using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace kOS
{
    public class ContextRunProgram : ExecutionContext
    {
        private int accumulator;
        private File file;
        private String commandBuffer;
        private List<Command> commands = new List<Command>();
        private List<Expression> parameters = new List<Expression>();
        private int executionLine = 0;

        
        
        public string Filename;

        public ContextRunProgram(ExecutionContext parent, List<Expression> parameters, String filename) : base(parent) 
        {
            this.parameters = parameters;
            this.Filename = filename;
        }

        public void Run(File file)
        {
            this.file = file;

            State = ExecutionState.WAIT;

            RunBlock(file);
        }

        private void RunBlock(List<String> block)
        {

            foreach (String rawLine in block)
            {
                String line = stripComment(rawLine);
                commandBuffer += line + "\n";
            }

            string cmd;
            int lineNumber = 0;
            int commandLineStart = 0;
            while (parseNext(ref commandBuffer, out cmd, ref lineNumber, out commandLineStart))
            {
                try
                {
                    Line = commandLineStart;
                    Command cmdObj = Command.Get(cmd, this, commandLineStart);
                    commands.Add(cmdObj);
                }
                catch (kOSException e)
                {
                    if (ParentContext.FindClosestParentOfType<ContextRunProgram>() != null)
                    {
                        // Error occurs in a child of another running program
                        StdOut("Error in '" + e.Program.Filename + "' on line " + e.LineNumber + ": " + e.Message);
                        State = ExecutionState.DONE;
                        return;
                    }
                    else
                    {
                        // Error occurs in the top level program
                        StdOut("Error on line " + e.LineNumber + ": " + e.Message);
                        State = ExecutionState.DONE;
                        return;
                    }
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
        }

        public override bool Break()
        {
            State = ExecutionState.DONE;

            return true;
        }

        public string stripComment(string line)
        {
            for (var i=0; i<line.Length; i++)
            {
                if (line[i] == '\"')
                {
                    i = Utils.FindEndOfString(line, i + 1);
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
            catch (kOSException e)
            {
                if (ParentContext.FindClosestParentOfType<ContextRunProgram>() != null)
                {
                    // Error occurs in a child of another running program
                    StdOut("Error in '" + e.Program.Filename + "' on line " + e.LineNumber + ": " + e.Message);
                    State = ExecutionState.DONE;
                    return;
                }
                else
                {
                    // Error occurs in the top level program
                    StdOut("Error on line " + e.LineNumber + ": " + e.Message);
                    State = ExecutionState.DONE;
                    return;
                }
            }
            catch (Exception e)
            {
                // Non-kos exception! This is a bug, but no reason to kill the OS
                StdOut("Flagrant error on line " + executionLine);
                UnityEngine.Debug.Log("Program error");
                UnityEngine.Debug.Log(e);
                State = ExecutionState.DONE;
                return;
            }
        }

        private void EvaluateNextCommand()
        {
            if (this.ChildContext == null)
            {
                if (commands.Count > 0)
                {
                    Command cmd = commands[0];
                    commands.RemoveAt(0);

                    ChildContext = cmd;
                    cmd.Evaluate();
                }
                else
                {
                    State = ExecutionState.DONE;
                }
            }
        }

        public object PopParameter()
        {
            if (parameters.Count > 0)
            {
                object retValue = parameters[0].GetValue();
                parameters.RemoveAt(0);

                return retValue;
            }

            throw new kOSException("Wrong number of parameters supplied");
        }
    }
}
