using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace kOS
{    
    [CommandAttribute("CLEARSCREEN")]
    public class CommandClearScreen : Command
    {
        public CommandClearScreen(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            ParentContext.SendMessage(SystemMessage.CLEARSCREEN);
            State = ExecutionState.DONE;
        }
    }

    [CommandAttribute("SHUTDOWN")]
    public class CommandShutdown : Command
    {
        public CommandShutdown(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            ParentContext.SendMessage(SystemMessage.SHUTDOWN);
            State = ExecutionState.DONE;
        }
    }
    
    [CommandAttribute("REBOOT")]
    public class CommandReboot : Command
    {
        public CommandReboot(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            ParentContext.SendMessage(SystemMessage.RESTART);
            State = ExecutionState.DONE;
        }
    }

    [CommandAttribute("PRINT * AT_(2)")]
    public class CommandPrintAt : Command
    {
        public CommandPrintAt(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            Expression e = new Expression(RegexMatch.Groups[1].Value, ParentContext);
            Expression ex = new Expression(RegexMatch.Groups[2].Value, ParentContext);
            Expression ey = new Expression(RegexMatch.Groups[3].Value, ParentContext);

            if (e.IsNull()) throw new kOSException("Null value in print statement");

            int x, y;

            if (Int32.TryParse(ex.ToString(), out x) && Int32.TryParse(ey.ToString(), out y))
            {
                Put(e.ToString(), x, y);
            }
            else
            {
                throw new kOSException("Non-numeric value assigned to numeric function", this);
            }

            State = ExecutionState.DONE;
        }
    }

    [CommandAttribute("PRINT *")]
    public class CommandPrint : Command
    {
        public CommandPrint(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            Expression e = new Expression(RegexMatch.Groups[1].Value, ParentContext);

            if (e.IsNull())
            {
                StdOut("NULL");
                State = ExecutionState.DONE;
            }
            else
            {
                StdOut(e.ToString());
                State = ExecutionState.DONE;
            }
        }
    }

    [CommandAttribute("TEST *")]
    public class CommandTestKegex : Command
    {
        public CommandTestKegex(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            Expression e = new Expression(RegexMatch.Groups[1].Value, ParentContext);

            StdOut(e.GetValue().ToString());

            State = ExecutionState.DONE;
        }
    }

    [CommandAttribute("DECLARE %")]
    public class CommandDeclareVar : Command
    {
        public CommandDeclareVar(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            String varName = RegexMatch.Groups[1].Value;
            Variable v = FindOrCreateVariable(varName);

            if (v == null) throw new kOSException("Can't create variable '" + varName + "'", this);
            
            State = ExecutionState.DONE;
        }
    }

    [CommandAttribute("DECLARE PARAMETERS? *")]
    public class CommandDeclareParameter : Command
    {
        public CommandDeclareParameter(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            if (!(ParentContext is ContextRunProgram)) throw new kOSException("DECLARE PARAMETERS can only be used within a program.", this);

            foreach (String varName in RegexMatch.Groups[1].Value.Split(','))
            {
                Variable v = FindOrCreateVariable(varName);
                if (v == null) throw new kOSException("Can't create variable '" + varName + "'", this);

                var program = (ContextRunProgram)ParentContext;
                v.Value = program.PopParameter();
            }

            State = ExecutionState.DONE;
        }
    }
    
    [CommandAttribute("SET % TO *")]
    public class CommandSet : Command
    {
        public CommandSet(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            String varName = RegexMatch.Groups[1].Value;
            Variable v = FindOrCreateVariable(varName);

            if (v != null)
            {
                Expression e = new Expression(RegexMatch.Groups[2].Value, ParentContext);
                v.Value = e.GetValue();

                State = ExecutionState.DONE;
            }
            else
            {
                throw new kOSException("Can't find or create variable '" + varName + "'", this);
            }
        }
    }
    
    [CommandAttribute("TOGGLE %")]
    public class CommandToggle : Command
    {
        public CommandToggle(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            String varName = RegexMatch.Groups[1].Value;
            Variable v = FindOrCreateVariable(varName);

            if (v != null)
            {
                if (v.Value is bool)
                {
                    v.Value = !((bool)v.Value);
                    State = ExecutionState.DONE;
                }
                else if (v.Value is float)
                {
                    bool val = ((float)v.Value > 0) ? true : false;
                    v.Value = !val;
                    State = ExecutionState.DONE;
                }
                else
                {
                    throw new kOSException("That variable can't be toggled.", this);
                }
            }
            else
            {
                throw new kOSException("Can't find or create variable '" + varName + "'", this);
            }
        }
    }

    [CommandAttribute("% ON")]
    public class CommandSetOn : Command
    {
        public CommandSetOn(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            String varName = RegexMatch.Groups[1].Value;
            Variable v = FindOrCreateVariable(varName);

            if (v != null)
            {
                if (v.Value is bool || v.Value is float)
                {
                    v.Value = true;
                    State = ExecutionState.DONE;
                }
                else
                {
                    throw new kOSException("That variable can't be set to 'ON'.", this);
                }
            }
            else
            {
                throw new kOSException("Can't find or create variable '" + varName + "'", this);
            }
        }
    }

    [CommandAttribute("% OFF")]
    public class CommandSetOff : Command
    {
        public CommandSetOff(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            String varName = RegexMatch.Groups[1].Value;
            Variable v = FindOrCreateVariable(varName);

            if (v != null)
            {
                if (v.Value is bool || v.Value is float)
                {
                    v.Value = false;
                    State = ExecutionState.DONE;
                }
                else
                {
                    throw new kOSException("That variable can't be set to 'OFF'.", this);
                }
            }
            else
            {
                throw new kOSException("Can't find or create variable '" + varName + "'", this);
            }
        }
    }
}
