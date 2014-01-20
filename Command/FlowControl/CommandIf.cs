using System.Text.RegularExpressions;
using kOS.Context;
using kOS.Utilities;

namespace kOS.Command.FlowControl
{
    [Command("IF /_{}")]
    public class CommandIf : Command
    {
        Expression.Expression expression;
        Command targetCommand;

        public CommandIf(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            expression = new Expression.Expression(RegexMatch.Groups[1].Value, ParentContext);

            var numLinesChild = Utils.NewLineCount(Input.Substring(0, RegexMatch.Groups[2].Index));
            targetCommand = Get(RegexMatch.Groups[2].Value, this, Line + numLinesChild);

            if (expression.IsTrue())
            {
                targetCommand.Evaluate();
                Push(targetCommand);
                State = ExecutionState.WAIT;
            }
            else
            {
                State = ExecutionState.DONE;
            }
        }

        public override void Update(float time)
        {
            base.Update(time);

            if (ChildContext != null && ChildContext.State != ExecutionState.DONE) return;

            ChildContext = null;
            State = ExecutionState.DONE;
        }
    }
}