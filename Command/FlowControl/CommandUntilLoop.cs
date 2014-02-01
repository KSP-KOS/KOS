using System.Text.RegularExpressions;
using kOS.Context;
using kOS.Utilities;

namespace kOS.Command.FlowControl
{
    [Command("UNTIL /_{}")]
    public class CommandUntilLoop : Command
    {
        // commandstring;
        private ICommand targetCommand;
        private Expression.Expression waitExpression;

        public CommandUntilLoop(Match regexMatch, IExecutionContext context) : base(regexMatch, context)
        {
        }

        public override void Evaluate()
        {
            waitExpression = new Expression.Expression(RegexMatch.Groups[1].Value, ParentContext);

            var numLinesChild = Utils.NewLineCount(Input.Substring(0, RegexMatch.Groups[2].Index));
            targetCommand = Get(RegexMatch.Groups[2].Value, this, Line + numLinesChild);

            //commandstring = RegexMatch.Groups[2].Value;

            State = ExecutionState.WAIT;
        }

        public override bool Break()
        {
            State = ExecutionState.DONE;

            return true;
        }

        public override bool SpecialKey(kOSKeys key)
        {
            if (key == kOSKeys.BREAK)
            {
                StdOut("Break.");
                Break();
            }

            return base.SpecialKey(key);
        }

        public override void Update(float time)
        {
            base.Update(time);

            if (ChildContext == null)
            {
                if (waitExpression.IsTrue())
                {
                    State = ExecutionState.DONE;
                }
                else
                {
                    ChildContext = targetCommand;
                    //ChildContext = Command.Get(commandstring, this);
                    ((ICommand) ChildContext).Evaluate();
                }
            }
            else
            {
                if (ChildContext != null && ChildContext.State == ExecutionState.DONE)
                {
                    ChildContext = null;
                }
            }
        }
    }
}