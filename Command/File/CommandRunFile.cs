using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace kOS.Command.File
{
    [Command(@"^RUN ([a-zA-Z0-9\-_]+?)( ?\((.*?)\))?$")]
    public class CommandRunFile : Command
    {
        public CommandRunFile(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }
          
        public override void Evaluate()
        {
            var fileName = RegexMatch.Groups[1].Value;
            var file = SelectedVolume.GetByName(fileName);
            var parameters = new List<Expression>();

            if (RegexMatch.Groups.Count > 1)
            {
                var paramString = RegexMatch.Groups[3].Value;
                parameters.AddRange(Utils.ProcessParams(paramString).Select(param => new Expression(param, this)));
            }

            if (file == null)
            {
                throw new kOSException("File not found '" + fileName + "'.", this);
            }

            var runContext = new ContextRunProgram(this, parameters, fileName);
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

        public override bool Type(char c)
        {
            return State == ExecutionState.WAIT || base.Type(c);
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
            base.Update(time);

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
}