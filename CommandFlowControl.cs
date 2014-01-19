using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;


namespace kOS
{
    [CommandAttribute("^{([\\S\\s]*)}$")]
    public class CommandBlock : Command
    {
        readonly List<Command> commands = new List<Command>();
        String commandBuffer = "";

        public CommandBlock(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public CommandBlock(String directInput, ExecutionContext context) : base(Regex.Match(directInput, "^([\\S\\s]*)$"), context) {}

        public override void Evaluate()
        {
            String innerText = RegexMatch.Groups[1].Value;
            String cmd;
            commandBuffer = innerText;
            int lineCount = Line;
            int commandLineStart = 0;

            if (commands.Count == 0)
            {
                while (parseNext(ref innerText, out cmd, ref lineCount, out commandLineStart))
                {
                    commands.Add(Get(cmd, this, commandLineStart));
                }
            }
            else
                Refresh();

            State = (commands.Count == 0) ? ExecutionState.DONE : ExecutionState.WAIT;
        }

        public override void Refresh()
        {
            base.Refresh();

            foreach (Command c in commands)
            {
                c.Refresh();
            }
        }

        public new void Break()
        {
            commands.Clear();
            State = ExecutionState.DONE;
        }

        public override void Update(float time)
        {
            foreach (var command in commands)
            {
                switch (command.State)
                {
                    case ExecutionState.NEW:
                        command.Evaluate();
                        ChildContext = command;
                        return;

                    case ExecutionState.WAIT:
                        command.Update(time);
                        return;
                }
            }

            State = ExecutionState.DONE;
        }
    }

    [CommandAttribute("IF /_{}")]
    public class CommandIf : Command
    {
        Expression expression;
        Command targetCommand;

        public CommandIf(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            expression = new Expression(RegexMatch.Groups[1].Value, ParentContext);

            int numLinesChild = Utils.NewLineCount(Input.Substring(0, RegexMatch.Groups[2].Index));
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

    [CommandAttribute("UNTIL /_{}")]
    public class CommandUntilLoop : Command
    {
        Expression waitExpression;
        // commandString;
        Command targetCommand;

        public CommandUntilLoop(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            UnityEngine.Debug.Log("For UNTIL: " + RegexMatch );

            for (var i = 0; i < RegexMatch.Groups.Count; i++)
            {
                var group = RegexMatch.Groups[1];
                UnityEngine.Debug.Log("Until Match Group V: " + group.Value + " I: " + group.Index + "-" + i);
            }
            waitExpression = new Expression(RegexMatch.Groups[1].Value, ParentContext);

            var numLinesChild = Utils.NewLineCount(Input.Substring(0, RegexMatch.Groups[2].Index));
            targetCommand = Get(RegexMatch.Groups[2].Value, this, Line + numLinesChild);

            //commandString = RegexMatch.Groups[2].Value;

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
                    //ChildContext = Command.Get(commandString, this);
                    ((Command)ChildContext).Evaluate();
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
    [CommandAttribute("FOR /_ IN /_ {}")]
    public class CommandForLoop : Command
    {
        // commandString;
        Command targetCommand;
        private Enumerator iterator;
        private string iteratorString;

        public CommandForLoop(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            var listName = RegexMatch.Groups[2].Value;
            iteratorString = RegexMatch.Groups[1].Value;

            var expression = new Expression(listName, ParentContext).GetValue();
            var list = expression as ListValue;
            iterator = list.GetSuffix("ITERATOR") as Enumerator;

            int numLinesChild = Utils.NewLineCount(Input.Substring(0, RegexMatch.Groups[2].Index));
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
                    ParentContext.RemoveVariable(iteratorString);
                }
                else
                {
                    var iteratorVariable = ParentContext.FindOrCreateVariable(iteratorString);
                    iteratorVariable.Value = iterator.GetSuffix("VALUE");
                    ChildContext = targetCommand;
                    //ChildContext = Command.Get(commandString, this);
                    ((Command)ChildContext).Evaluate();
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

    [CommandAttribute("BREAK")]
    public class CommandBreak : Command
    {
        public CommandBreak(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            Break();
            State = ExecutionState.DONE;
        }
    }
}
