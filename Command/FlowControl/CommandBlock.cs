using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace kOS.Command
{
    [Command("^{([\\S\\s]*)}$")]
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
}