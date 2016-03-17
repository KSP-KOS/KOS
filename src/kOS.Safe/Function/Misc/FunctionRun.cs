using kOS.Safe.Compilation;
using kOS.Safe.Encapsulation;
using kOS.Safe.Execution;
using kOS.Safe.Persistence;
using System;
using System.Collections.Generic;

namespace kOS.Safe.Function.Misc
{
    [Function("run")]
    public class FunctionRun : SafeFunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            // run() is strange.  It needs two levels of args - the args to itself, and the args it is meant to
            // pass on to the program it's invoking.  First, these are the args to run itself:
            object volumeId = PopValueAssert(shared, true);
            string fileName = PopValueAssert(shared, true).ToString();
            AssertArgBottomAndConsume(shared);

            // Now the args it is going to be passing on to the program:
            var progArgs = new List<object>();
            int argc = CountRemainingArgs(shared);
            for (int i = 0; i < argc; ++i)
                progArgs.Add(PopValueAssert(shared, true));
            AssertArgBottomAndConsume(shared);

            if (shared.VolumeMgr == null) return;
            if (shared.VolumeMgr.CurrentVolume == null) throw new Exception("Volume not found");

            VolumeFile file = shared.VolumeMgr.CurrentVolume.Open(fileName, true);
            if (file == null) throw new Exception(string.Format("File '{0}' not found", fileName));
            if (shared.ScriptHandler == null) return;

            if (volumeId != null)
            {
                throw new kOS.Safe.Exceptions.KOSException("Running programs on another processor is not supported");
            }
            else
            {
                // clear the "program" compilation context
                shared.Cpu.StartCompileStopwatch();
                shared.ScriptHandler.ClearContext("program");
                string filePath = shared.VolumeMgr.GetVolumeRawIdentifier(shared.VolumeMgr.CurrentVolume) + "/" + fileName;
                var options = new CompilerOptions { LoadProgramsInSameAddressSpace = true, FuncManager = shared.FunctionManager };
                var programContext = shared.Cpu.SwitchToProgramContext();

                List<CodePart> codeParts;
                FileContent content = file.ReadAll();
                if (content.Category == FileCategory.KSM)
                {
                    string prefix = programContext.Program.Count.ToString();
                    codeParts = content.AsParts(fileName, prefix);
                }
                else
                {
                    try
                    {
                        codeParts = shared.ScriptHandler.Compile(filePath, 1, content.String, "program", options);
                    }
                    catch (Exception)
                    {
                        // If it died due to a compile error, then we won't really be able to switch to program context
                        // as was implied by calling Cpu.SwitchToProgramContext() up above.  The CPU needs to be
                        // told that it's still in interpreter context, or else it fails to advance the interpreter's
                        // instruction pointer and it will just try the "call run()" instruction again:
                        shared.Cpu.BreakExecution(false);
                        throw;
                    }
                }
                programContext.AddParts(codeParts);
                shared.Cpu.StopCompileStopwatch();
            }

            // Because run() returns FIRST, and THEN the CPU jumps to the new program's first instruction that it set up,
            // it needs to put the return stack in a weird order.  Its return value needs to be buried UNDER the args to the
            // program it's calling:
            UsesAutoReturn = false;

            shared.Cpu.PushStack(0); // dummy return that all functions have.

            // Put the args for the program being called back on in the same order they were in before (so read the list backward):
            shared.Cpu.PushStack(new KOSArgMarkerType());
            for (int i = argc - 1; i >= 0; --i)
                shared.Cpu.PushStack(progArgs[i]);
        }
    }
}