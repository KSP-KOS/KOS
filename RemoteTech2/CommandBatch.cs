using System.Text.RegularExpressions;
using kOS.Command;
using kOS.Context;

namespace kOS.RemoteTech2
{
    // Cilph: RemoteTech-exclusive command that combines copy and execute.
    [Command("BATCH")]
    public class CommandBatch : Command.Command
    {
        public CommandBatch(Match regexMatch, IExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            var parent = ParentContext as Interpreter.ImmediateMode;
            if (parent == null) throw new Debug.KOSException("Batch mode can only be used when in immediate mode.", this);
            if (parent.BatchMode) throw new Debug.KOSException("Batch mode is already active.", this);
            parent.BatchMode = true;
            StdOut("Starting new batch.");
            State = ExecutionState.DONE;
        }
    }
}