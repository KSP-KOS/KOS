using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace kOS
{
    [CommandAttribute("WAIT[UNTIL]? *")]
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

    [CommandAttribute("ON % *")]
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
    
    [CommandAttribute("LOCK % TO *")]
    public class CommandLock : Command
    {
        public CommandLock(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            string varname = RegexMatch.Groups[1].Value;
            Expression expression = new Expression(RegexMatch.Groups[2].Value, ParentContext);

            ParentContext.Unlock(varname);
            ParentContext.Lock(varname, expression);

            State = ExecutionState.DONE;
        }
    }

    [CommandAttribute("UNLOCK %")]
    public class CommandUnlock : Command
    {
        public CommandUnlock(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            String varname = RegexMatch.Groups[1].Value;

            if (varname.ToUpper() == "ALL")
            {
                ParentContext.UnlockAll();
            }
            else
            {
                ParentContext.Unlock(varname);
            }

            State = ExecutionState.DONE;
        }
    }
    
    [CommandAttribute("WHEN ~ THEN *")]
    public class CommandWhen : Command
    {
        private Command targetCommand;
        private Expression expression;
        private bool triggered = false;

        public CommandWhen(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            expression = new Expression(RegexMatch.Groups[1].Value, ParentContext);
            targetCommand = Command.Get(RegexMatch.Groups[2].Value, ParentContext);

            ParentContext.Lock(this);

            State = ExecutionState.DONE;
        }

        public override void Update(float time)
        {
            if (triggered)
            {
                targetCommand.Update(time);
                if (targetCommand.State == ExecutionState.DONE) ParentContext.Unlock(this);
            }
            else if (expression.IsTrue())
            {
                triggered = true;
                targetCommand.Evaluate();
            }
        }
    }
}
