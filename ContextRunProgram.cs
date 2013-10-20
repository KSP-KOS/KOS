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

        public ContextRunProgram(ExecutionContext parent, List<Expression> parameters) : base(parent) 
        {
            this.parameters = parameters;
        }

        public void Run(File file)
        {
            this.file = file;

            State = ExecutionState.WAIT;

            int lineNumber = 0;
            foreach (String rawLine in file)
            {
                String line = stripComment(rawLine);

				if (!Utils.DelimterMatch (line)) 
				{
					throw new kOSException ("line" + lineNumber +": mismatching delimiter.");
				}

                commandBuffer += line;

                string cmd;
                while (parseNext(ref commandBuffer, out cmd))
                {
                    try
                    {
                        Command cmdObj = Command.Get(cmd, this);
                        cmdObj.LineNumber = lineNumber;
                        commands.Add(cmdObj);
                    }
                    catch (kOSException e)
                    {
                        StdOut("Error on line " + lineNumber + ": " + e.Message);
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

                lineNumber++;
                accumulator++;
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
                    i = Expression.FindEndOfString(line, i + 1);
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
            base.Update(time);

            try
            {
                EvaluateNextCommand();
            }
            catch (kOSException e)
            {
                StdOut("Error on line " + executionLine + ": " + e.Message);
                State = ExecutionState.DONE;
                return;
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
                    executionLine = cmd.LineNumber;
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
