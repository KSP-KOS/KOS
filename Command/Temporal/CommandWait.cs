using System.Text.RegularExpressions;

namespace kOS.Command
{
    [Command("WAIT[UNTIL]? *")]
    public class CommandWait : Command
    {
        public CommandWait(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        private float waitTime = 0;
        private Expression waitExpression = null;

        public override void Evaluate()
        {
            Expression e = new Expression(RegexMatch.Groups[2].Value, ParentContext);
            bool untilClause = (RegexMatch.Groups[1].Value.Trim().ToUpper() == "UNTIL");

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
            if (State == ExecutionState.WAIT)
            {
                return true;
            }
            else
            {
                return base.Type(c);
            }
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