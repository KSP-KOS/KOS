using System;
using System.Text.RegularExpressions;
using kOS.Command;
using kOS.Context;
using kOS.Interpreter;

namespace kOS.RemoteTech2
{

    [Command("DEPLOY")]
    public class CommandDeploy : Command.Command
    {
        public CommandDeploy(Match regexMatch, IExecutionContext context) : base(regexMatch, context) { }

        private double waitTotal, waitElapsed;

        public override void Evaluate()
        {
            var parent = ParentContext as ImmediateMode;
            if (parent == null) throw new Debug.KOSException("Batch mode can only be used when in immediate mode.", this);
            if (!parent.BatchMode) throw new Debug.KOSException("Batch mode is not active.", this);
            StdOut("Deploying...");

            if (RemoteTechHook.Instance != null)
            {
                waitTotal = RemoteTechHook.Instance.GetShortestSignalDelay(Vessel.id);
                if (double.IsPositiveInfinity(waitTotal)) throw new Debug.KOSException("No connection available.");
            }

            State = ExecutionState.WAIT;
        }

        public override void Update(float time)
        {
            if (waitElapsed >= waitTotal)
            {
                var parent = ParentContext as ImmediateMode;
                if (parent != null) parent.BatchMode = false;
                State = ExecutionState.DONE;
            }

            if (RemoteTechHook.Instance != null && !RemoteTechHook.Instance.HasAnyConnection(Vessel.id))
                throw new Debug.KOSException("Signal interruption. Transmission lost.");

            if (!(waitElapsed < waitTotal)) return;

            waitElapsed += Math.Min(time, waitTotal - waitElapsed);
            this.DrawProgressBar(waitElapsed, waitTotal, "Deploying batch.");
        }
    }
}
