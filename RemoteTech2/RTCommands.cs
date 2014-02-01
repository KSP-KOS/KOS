using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using kOS.Context;
using kOS.Interpreter;

namespace kOS.Command
{
    // Cilph: RemoteTech-exclusive command that combines copy and execute.
    [CommandAttribute("BATCH")]
    public class CommandBatch : Command
    {
        public CommandBatch(Match regexMatch, IExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            var parent = ParentContext as kOS.Interpreter.ImmediateMode;
            if (parent == null) throw new kOS.Debug.KOSException("Batch mode can only be used when in immediate mode.", this);
            if (parent.BatchMode) throw new kOS.Debug.KOSException("Batch mode is already active.", this);
            parent.BatchMode = true;
            StdOut("Starting new batch.");
            State = ExecutionState.DONE;
        }
    }

    [CommandAttribute("DEPLOY")]
    public class CommandDeploy : Command
    {
        public CommandDeploy(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public double waitTotal, waitElapsed;

        public override void Evaluate()
        {
            var parent = ParentContext as ImmediateMode;
            if (parent == null) throw new kOS.Debug.KOSException("Batch mode can only be used when in immediate mode.", this);
            if (!parent.BatchMode) throw new kOS.Debug.KOSException("Batch mode is not active.", this);
            StdOut("Deploying...");

            if (RTHook.Instance != null)
            {
                waitTotal = RTHook.Instance.GetShortestSignalDelay(Vessel.id);
                if (waitTotal == Double.PositiveInfinity) throw new kOS.Debug.KOSException("No connection available.");
            }

            State = ExecutionState.WAIT;
        }

        public override void Update(float time)
        {
            if (waitElapsed >= waitTotal)
            {
                var parent = ParentContext as ImmediateMode;
                parent.BatchMode = false;
                State = ExecutionState.DONE;
            }

            if (RTHook.Instance != null && !RTHook.Instance.HasAnyConnection(Vessel.id))
                throw new kOS.Debug.KOSException("Signal interruption. Transmission lost.");

            if (waitElapsed < waitTotal)
            {
                waitElapsed += Math.Min(time, waitTotal - waitElapsed);
                this.DrawProgressBar(waitElapsed, waitTotal, "Deploying batch.");
            }
        }
    }
}
