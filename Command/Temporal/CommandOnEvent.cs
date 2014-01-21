using System;
using System.Text.RegularExpressions;
using kOS.Binding;
using kOS.Context;

namespace kOS.Command.Temporal
{
    [Command("ON % *")]
    public class CommandOnEvent : Command
    {
        private Variable targetVariable;
        private ICommand targetCommand;
        private bool originalValue;

        public CommandOnEvent(Match regexMatch, IExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            targetVariable = ParentContext.FindOrCreateVariable(RegexMatch.Groups[1].Value);
            targetCommand = Get(RegexMatch.Groups[2].Value, ParentContext);

            if (!ObjToBool(targetVariable.Value, out originalValue))
            {
                throw new Exception("Value type error");
            }

            ParentContext.Lock(this);

            State = ExecutionState.DONE;
        }

        public override void Update(float time)
        {
            bool newValue;
            if (!ObjToBool(targetVariable.Value, out newValue))
            {
                ParentContext.Unlock(this);

                throw new Exception("Value type error");
            }

            if (originalValue == newValue) return;
            ParentContext.Unlock(this);

            targetCommand.Evaluate();
            ParentContext.Push(targetCommand);
        }

        public bool ObjToBool(object obj, out bool result)
        {
            if (bool.TryParse(targetVariable.Value.ToString(), out result))
            {
                return true;
            }
            if (obj is float)
            {
                result = ((float)obj) > 0;
                return true;
            }

            return false;
        }
    }
}