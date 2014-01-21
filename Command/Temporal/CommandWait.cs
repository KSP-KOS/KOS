using System.Text.RegularExpressions;
using kOS.Context;
using kOS.Utilities;

namespace kOS.Command.Temporal
{
    [Command("WAIT[UNTIL]? *")]
    public class CommandWait : Command
    {
        public CommandWait(Match regexMatch, IExecutionContext context) : base(regexMatch, context) { }

        private float waitTime;
        private Expression.Expression waitExpression;

        public override void Evaluate()
        {
            var e = new Expression.Expression(RegexMatch.Groups[2].Value, ParentContext);
            var untilClause = (RegexMatch.Groups[1].Value.Trim().ToUpper() == "UNTIL");

            if (!untilClause)
            {
                waitTime = e.Float();
            }
            else
            {
                waitExpression = e;
            }

            State = ExecutionState.WAIT;
        }

        public override bool SpecialKey(kOSKeys key)
        {
            if (key == kOSKeys.BREAK)
            {
                StdOut("Break.");
                State = ExecutionState.DONE;
            }

            return base.SpecialKey(key);
        }

        public override bool Type(char c)
        {
            return State == ExecutionState.WAIT || base.Type(c);
        }

        public override void Update(float time)
        {
            if (waitExpression != null)
            {
                if (waitExpression.IsTrue())
                {
                    State = ExecutionState.DONE;
                }
            }
            else if (waitTime > 0)
            {
                waitTime -= time;
                if (waitTime <= 0) State = ExecutionState.DONE;
            }
        }
    }
}