using System;
using System.Text.RegularExpressions;

namespace kOS.Command
{
    [Command("ON % *")]
    public class CommandOnEvent : Command
    {
        private Variable targetVariable;
        private Command targetCommand;
        private bool originalValue;

        public CommandOnEvent(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            targetVariable = ParentContext.FindOrCreateVariable(RegexMatch.Groups[1].Value);
            targetCommand = Command.Get(RegexMatch.Groups[2].Value, ParentContext);

            if (!objToBool(targetVariable.Value, out originalValue))
            {
                throw new Exception("Value type error");
            }

            ParentContext.Lock(this);

            State = ExecutionState.DONE;
        }

        public override void Update(float time)
        {
            bool newValue;
            if (!objToBool(targetVariable.Value, out newValue))
            {
                ParentContext.Unlock(this);

                throw new Exception("Value type error");
            }

            if (originalValue != newValue)
            {
                ParentContext.Unlock(this);

                targetCommand.Evaluate();
                ParentContext.Push(targetCommand);
            }
        }

        public bool objToBool(object obj, out bool result)
        {
            if (bool.TryParse(targetVariable.Value.ToString(), out result))
            {
                return true;
            }
            else
            {
                if (obj is float)
                {
                    result = ((float)obj) > 0;
                    return true;
                }
            }

            return false;
        }
    }
}