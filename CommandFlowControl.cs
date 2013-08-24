using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace kOS
{
    [CommandAttribute(@"^{(.*)}$")]
    public class CommandBlock : Command
    {
        List<Command> commands = new List<Command>();
        String commandBuffer = "";

        public CommandBlock(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            String innerText = RegexMatch.Groups[1].Value;
            String cmd;
            commandBuffer = innerText;

            while (parseNext(ref innerText, out cmd))
            {
                commands.Add(Command.Get(cmd, this));
            }

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

        public override void Update(float time)
        {
            foreach (Command command in commands)
            {
                switch (command.State)
                {
                    case ExecutionState.NEW:
                        command.Evaluate();
                        return;

                    case ExecutionState.WAIT:
                        command.Update(time);
                        return;
                }
            }

            State = ExecutionState.DONE;
        }
    }

    [CommandAttribute(@"^IF (.*)({.*})$")]
    public class CommandIf : Command
    {
        List<Command> commands = new List<Command>();
        Expression waitExpression;
        String commandString;

        public CommandIf(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            waitExpression = new Expression(RegexMatch.Groups[1].Value, ParentContext);
            commandString = RegexMatch.Groups[2].Value;

            State = ExecutionState.WAIT;
        }
    }

    [CommandAttribute(@"^UNTIL (.*)({.*})$")]
    public class CommandUntilLoop : Command
    {
        List<Command> commands = new List<Command>();
        Expression waitExpression;
        String commandString;

        public CommandUntilLoop(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            waitExpression = new Expression(RegexMatch.Groups[1].Value, ParentContext);
            commandString = RegexMatch.Groups[2].Value;

            State = ExecutionState.WAIT;
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
                    ChildContext = Command.Get(commandString, this);
                    ((Command)ChildContext).Evaluate();
                }
            }
            else
            {
                if (ChildContext.State == ExecutionState.DONE)
                {
                    ChildContext = null;
                }
            }
        }
    }
}
