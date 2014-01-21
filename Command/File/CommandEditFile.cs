using System.Text.RegularExpressions;
using kOS.Context;
using kOS.Debug;
using kOS.Interpreter;

namespace kOS.Command.File
{
    [Command("EDIT &")]
    public class CommandEditFile : Command
    {
        public CommandEditFile(Match regexMatch, IExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            var fileName = RegexMatch.Groups[1].Value;

            if (ParentContext is ImmediateMode)
            {
                ParentContext.Push(new InterpreterEdit(fileName, ParentContext));
            }
            else
            {
                throw new KOSException("Edit can only be used when in immediate mode.", this);
            }
        }
    }
}