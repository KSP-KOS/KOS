using System;
using System.Text.RegularExpressions;
using kOS.Context;
using kOS.Debug;

namespace kOS.Command.BasicIO
{
    [Command("PRINT * AT_(2)")]
    public class CommandPrintAt : Command
    {
        public CommandPrintAt(Match regexMatch, IExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            var e = new Expression.Expression(RegexMatch.Groups[1].Value, ParentContext);
            var ex = new Expression.Expression(RegexMatch.Groups[2].Value, ParentContext);
            var ey = new Expression.Expression(RegexMatch.Groups[3].Value, ParentContext);

            if (e.IsNull()) throw new KOSException("Null value in print statement");

            int x, y;

            if (Int32.TryParse(ex.ToString(), out x) && Int32.TryParse(ey.ToString(), out y))
            {
                Put(e.ToString(), x, y);
            }
            else
            {
                throw new KOSException("Non-numeric value assigned to numeric function", this);
            }

            State = ExecutionState.DONE;
        }
    }
}