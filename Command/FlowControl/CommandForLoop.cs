using System.Text.RegularExpressions;
using kOS.Context;
using kOS.Debug;
using kOS.Suffixed;
using kOS.Utilities;

namespace kOS.Command.FlowControl
{
    [Command("FOR /_ IN /_ {}")]
    public class CommandForLoop : Command
    {
        // commandstring;
        ICommand targetCommand;
        private Enumerator iterator;
        private string iteratorstring;

        public CommandForLoop(Match regexMatch, IExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            var listName = RegexMatch.Groups[2].Value;
            iteratorstring = RegexMatch.Groups[1].Value;

            var expression = new Expression.Expression(listName, ParentContext).GetValue();
            var list = expression as ListValue;
            if (list != null)
            {
                iterator = list.GetSuffix("ITERATOR") as Enumerator;
            }
            else
            {
                throw new KOSException(string.Format("List {0} Not Found.", listName));
            }

            var numLinesChild = Utils.NewLineCount(Input.Substring(0, RegexMatch.Groups[2].Index));
            targetCommand = Get(RegexMatch.Groups[3].Value, this, Line + numLinesChild);

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
                if ((bool)iterator.GetSuffix("END"))
                {
                    State = ExecutionState.DONE;
                    ParentContext.Unset(iteratorstring);
                }
                else
                {
                    var iteratorVariable = ParentContext.FindOrCreateVariable(iteratorstring);
                    iteratorVariable.Value = iterator.GetSuffix("VALUE");
                    ChildContext = targetCommand;
                    //ChildContext = Command.Get(commandstring, this);
                    ((ICommand)ChildContext).Evaluate();
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