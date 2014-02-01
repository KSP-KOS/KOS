using System.Collections.Generic;
using System.Text.RegularExpressions;
using kOS.Context;

namespace kOS.Command.FlowControl
{
    [Command("^{([\\S\\s]*)}$")]
    public class CommandBlock : Command
    {
        private readonly List<ICommand> commands = new List<ICommand>();
        private string commandBuffer = "";

        public CommandBlock(Match regexMatch, IExecutionContext context) : base(regexMatch, context)
        {
        }

        public CommandBlock(string directInput, IExecutionContext context)
            : base(Regex.Match(directInput, "^([\\S\\s]*)$"), context)
        {
        }

        public override void Evaluate()
        {
            var innerText = RegexMatch.Groups[1].Value;
            commandBuffer = innerText;
            var lineCount = Line;

            if (commands.Count == 0)
            {
                int commandLineStart;
                string cmd;
                while (ParseNext(ref innerText, out cmd, ref lineCount, out commandLineStart))
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

            foreach (var c in commands)
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
}