using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Function;
using kOS.Screen;

namespace kOS.AddOns.RemoteTech2
{
    [FunctionAttribute("batch")]
    public class FunctionBatch : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            // TODO: check if executing on the interpreter
            //var parent = ParentContext as Interpreter.ImmediateMode;
            //if (parent == null) throw new Debug.KOSException("Batch mode can only be used when in immediate mode.", this);

            Interpreter interpreter = shared.Interpreter;

            if (interpreter != null)
            {
                if (interpreter.BatchMode) throw new Exception("Batch mode is already active.");
                interpreter.BatchMode = true;
                shared.Screen.Print("Starting new batch.");
            }

        }
    }

    [FunctionAttribute("deploy")]
    public class FunctionDeploy : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            
        }
    }
}
