﻿using kOS.Execution;
using kOS.Safe.Compilation;
using kOS.Safe.Exceptions;
using kOS.Safe.Function;
using kOS.Safe.Module;
using kOS.Safe.Persistence;
using kOS.Safe.Utilities;
using kOS.Suffixed;
using System;
using System.Collections.Generic;

namespace kOS.Function
{
    [Function("clearscreen")]
    public class FunctionClearScreen : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            AssertArgBottomAndConsume(shared);
            shared.Window.ClearScreen();
        }
    }

    [Function("print")]
    public class FunctionPrint : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string textToPrint = PopValueAssert(shared).ToString();
            AssertArgBottomAndConsume(shared);
            shared.Screen.Print(textToPrint);
        }
    }

    [Function("hudtext")]
    public class FunctionHudText : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            bool      echo      = Convert.ToBoolean(PopValueAssert(shared));
            RgbaColor rgba      = GetRgba(PopValueAssert(shared));
            int       size      = Convert.ToInt32 (PopValueAssert(shared));
            int       style     = Convert.ToInt32 (PopValueAssert(shared));
            int       delay     = Convert.ToInt32 (PopValueAssert(shared));
            string    textToHud = PopValueAssert(shared).ToString();
            AssertArgBottomAndConsume(shared);
            string htmlColour = rgba.ToHexNotation();
            switch (style)
            {
                case 1:
                    ScreenMessages.PostScreenMessage("<color=" + htmlColour + "><size=" + size + ">" + textToHud + "</size></color>", delay, ScreenMessageStyle.UPPER_LEFT);
                    break;

                case 2:
                    ScreenMessages.PostScreenMessage("<color=" + htmlColour + "><size=" + size + ">" + textToHud + "</size></color>", delay, ScreenMessageStyle.UPPER_CENTER);
                    break;

                case 3:
                    ScreenMessages.PostScreenMessage("<color=" + htmlColour + "><size=" + size + ">" + textToHud + "</size></color>", delay, ScreenMessageStyle.UPPER_RIGHT);
                    break;

                case 4:
                    ScreenMessages.PostScreenMessage("<color=" + htmlColour + "><size=" + size + ">" + textToHud + "</size></color>", delay, ScreenMessageStyle.LOWER_CENTER);
                    break;

                default:
                    ScreenMessages.PostScreenMessage("*" + textToHud, 3f, ScreenMessageStyle.UPPER_CENTER);
                    break;
            }
            if (echo)
            {
                shared.Screen.Print("HUD: " + textToHud);
            }
        }
    }

    [Function("printat")]
    public class FunctionPrintAt : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            int row = Convert.ToInt32(PopValueAssert(shared));
            int column = Convert.ToInt32(PopValueAssert(shared));
            string textToPrint = PopValueAssert(shared).ToString();
            AssertArgBottomAndConsume(shared);
            shared.Screen.PrintAt(textToPrint, row, column);
        }
    }

    [Function("toggleflybywire")]
    public class FunctionToggleFlyByWire : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            bool enabled = Convert.ToBoolean(PopValueAssert(shared));
            string paramName = PopValueAssert(shared).ToString();
            AssertArgBottomAndConsume(shared);
            ((CPU)shared.Cpu).ToggleFlyByWire(paramName, enabled);
        }
    }

    [Function("selectautopilotmode")]
    public class FunctionSelectAutopilotMode : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string autopilotMode = PopValueAssert(shared).ToString();
            AssertArgBottomAndConsume(shared);
            ((CPU)shared.Cpu).SelectAutopilotMode(autopilotMode);
        }
    }

    [Function("stage")]
    public class FunctionStage : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            AssertArgBottomAndConsume(shared);
            if (Staging.separate_ready && shared.Vessel.isActiveVessel)
            {
                Staging.ActivateNextStage();
            }
            else if (!Staging.separate_ready)
            {
                SafeHouse.Logger.Log("FAIL SILENT: Stage is called before it is ready, Use STAGE:READY to check first if staging rapidly");
            }
            else if (!shared.Vessel.isActiveVessel)
            {
                throw new KOSCommandInvalidHereException("STAGE", "a non-active SHIP, KSP does not support this", "Core is on the active vessel");
            }
        }
    }

    [Function("run")]
    public class FunctionRun : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            // run() is strange.  It needs two levels of args - the args to itself, and the args it is meant to
            // pass on to the program it's invoking.  First, these are the args to run itself:
            object volumeId = PopValueAssert(shared, true);
            string fileName = PopValueAssert(shared, true).ToString();
            AssertArgBottomAndConsume(shared);

            // Now the args it is going to be passing on to the program:
            var progArgs = new List<Object>();
            int argc = CountRemainingArgs(shared);
            for (int i = 0; i < argc; ++i)
                progArgs.Add(PopValueAssert(shared, true));
            AssertArgBottomAndConsume(shared);

            if (shared.VolumeMgr == null) return;
            if (shared.VolumeMgr.CurrentVolume == null) throw new Exception("Volume not found");

            ProgramFile file = shared.VolumeMgr.CurrentVolume.GetByName(fileName, true);
            if (file == null) throw new Exception(string.Format("File '{0}' not found", fileName));
            if (shared.ScriptHandler == null) return;

            if (volumeId != null)
            {
                Volume targetVolume = shared.VolumeMgr.GetVolume(volumeId);
                if (targetVolume != null)
                {
                    if (shared.ProcessorMgr != null)
                    {
                        string filePath = string.Format("{0}/{1}", shared.VolumeMgr.GetVolumeRawIdentifier(targetVolume), fileName);
                        var options = new CompilerOptions { LoadProgramsInSameAddressSpace = true, FuncManager = shared.FunctionManager };
                        List<CodePart> parts = shared.ScriptHandler.Compile(filePath, 1, file.StringContent, "program", options);
                        var builder = new ProgramBuilder();
                        builder.AddRange(parts);
                        List<Opcode> program = builder.BuildProgram();
                        shared.ProcessorMgr.RunProgramOn(program, targetVolume);
                    }
                }
                else
                {
                    throw new KOSFileException("Volume not found");
                }
            }
            else
            {
                // clear the "program" compilation context
                shared.ScriptHandler.ClearContext("program");
                string filePath = shared.VolumeMgr.GetVolumeRawIdentifier(shared.VolumeMgr.CurrentVolume) + "/" + fileName;
                var options = new CompilerOptions { LoadProgramsInSameAddressSpace = true, FuncManager = shared.FunctionManager };
                var programContext = ((CPU)shared.Cpu).SwitchToProgramContext();

                List<CodePart> codeParts;
                if (file.Category == FileCategory.KSM)
                {
                    string prefix = programContext.Program.Count.ToString();
                    codeParts = shared.VolumeMgr.CurrentVolume.LoadObjectFile(filePath, prefix, file.BinaryContent);
                }
                else
                {
                    try
                    {
                        codeParts = shared.ScriptHandler.Compile(filePath, 1, file.StringContent, "program", options);
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
            }

            // Because run() returns FIRST, and THEN the CPU jumps to the new program's first instruction that it set up,
            // it needs to put the return stack in a weird order.  Its return value needs to be buried UNDER the args to the
            // program it's calling:
            UsesAutoReturn = false;

            shared.Cpu.PushStack(0); // dummy return that all functions have.

            // Put the args for the program being called back on in the same order they were in before (so read the list backward):
            for (int i = argc - 1; i >= 0; --i)
                shared.Cpu.PushStack(progArgs[i]);
        }
    }

    [FunctionAttribute("load")]
    public class FunctionLoad : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            bool defaultOutput = false;
            bool justCompiling = false; // is this load() happening to compile, or to run?
            string fileNameOut = null;
            object topStack = PopValueAssert(shared, true); // null if there's no output file (output file means compile, not run).
            if (topStack != null)
            {
                justCompiling = true;
                string outputArg = topStack.ToString();
                if (outputArg.Equals("-default-compile-out-"))
                    defaultOutput = true;
                else
                    fileNameOut = PersistenceUtilities.CookedFilename(outputArg, Volume.KOS_MACHINELANGUAGE_EXTENSION);
            }

            string fileName = null;
            topStack = PopValueAssert(shared, true);
            if (topStack != null)
                fileName = topStack.ToString();

            AssertArgBottomAndConsume(shared);

            if (fileName == null)
                throw new KOSFileException("No filename to load was given.");

            ProgramFile file = shared.VolumeMgr.CurrentVolume.GetByName(fileName, (!justCompiling)); // if running, look for KSM first.  If compiling look for KS first.
            if (file == null) throw new KOSFileException(string.Format("Can't find file '{0}'.", fileName));
            fileName = file.Filename; // just in case GetByName picked an extension that changed it.

            // filename is now guaranteed to have an extension.  To make default output name, replace the extension with KSM:
            if (defaultOutput)
                fileNameOut = fileName.Substring(0, fileName.LastIndexOf('.')) + "." + Volume.KOS_MACHINELANGUAGE_EXTENSION;

            if (fileNameOut != null && fileName == fileNameOut)
                throw new KOSFileException("Input and output filenames must differ.");

            if (shared.VolumeMgr == null) return;
            if (shared.VolumeMgr.CurrentVolume == null) throw new KOSFileException("Volume not found");

            if (shared.ScriptHandler != null)
            {
                var options = new CompilerOptions { LoadProgramsInSameAddressSpace = true, FuncManager = shared.FunctionManager };
                string filePath = shared.VolumeMgr.GetVolumeRawIdentifier(shared.VolumeMgr.CurrentVolume) + "/" + fileName;
                // add this program to the address space of the parent program,
                // or to a file to save:
                if (justCompiling)
                {
                    List<CodePart> compileParts = shared.ScriptHandler.Compile(filePath, 1, file.StringContent, String.Empty, options);
                    bool success = shared.VolumeMgr.CurrentVolume.SaveObjectFile(fileNameOut, compileParts);
                    if (!success)
                    {
                        throw new KOSFileException("Can't save compiled file: not enough space or access forbidden");
                    }
                }
                else
                {
                    var programContext = ((CPU)shared.Cpu).SwitchToProgramContext();
                    List<CodePart> parts;
                    if (file.Category == FileCategory.KSM)
                    {
                        string prefix = programContext.Program.Count.ToString();
                        parts = shared.VolumeMgr.CurrentVolume.LoadObjectFile(filePath, prefix, file.BinaryContent);
                    }
                    else
                    {
                        parts = shared.ScriptHandler.Compile(filePath, 1, file.StringContent, "program", options);
                    }
                    int programAddress = programContext.AddObjectParts(parts);
                    // push the entry point address of the new program onto the stack
                    shared.Cpu.PushStack(programAddress);
                }
            }
        }
    }

    [Function("add")]
    public class FunctionAddNode : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var node = (Node)PopValueAssert(shared);
            AssertArgBottomAndConsume(shared);
            node.AddToVessel(shared.Vessel);
        }
    }

    [Function("remove")]
    public class FunctionRemoveNode : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var node = (Node)PopValueAssert(shared);
            AssertArgBottomAndConsume(shared);
            node.Remove();
        }
    }

    [Function("logfile")]
    public class FunctionLogFile : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string fileName = PopValueAssert(shared, true).ToString();
            string expressionResult = PopValueAssert(shared).ToString();
            AssertArgBottomAndConsume(shared);

            if (shared.VolumeMgr != null)
            {
                Volume volume = shared.VolumeMgr.CurrentVolume;
                if (volume != null)
                {
                    if (!volume.AppendToFile(fileName, expressionResult))
                    {
                        throw new KOSFileException("Can't append to file: not enough space or access forbidden");
                    }
                }
                else
                {
                    throw new KOSFileException("Volume not found");
                }
            }
        }
    }

    [Function("reboot")]
    public class FunctionReboot : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            if (shared.Processor != null)
            {
                AssertArgBottomAndConsume(shared); // not sure if this matters when rebooting anwyway.
                shared.Processor.SetMode(ProcessorModes.OFF);
                shared.Processor.SetMode(ProcessorModes.READY);
                ((CPU)shared.Cpu).GetCurrentOpcode().AbortProgram = true;
            }
        }
    }

    [Function("shutdown")]
    public class FunctionShutdown : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            AssertArgBottomAndConsume(shared); // not sure if this matters when shutting down anwyway.
            if (shared.Processor != null) shared.Processor.SetMode(ProcessorModes.OFF);
            ((CPU)shared.Cpu).GetCurrentOpcode().AbortProgram = true;
        }
    }

    [Function("debugdump")]
    public class DebugDump : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            AssertArgBottomAndConsume(shared);
            ReturnValue = shared.Cpu.DumpVariables();
        }
    }

    [Function("warpto")]
    public class WarpTo : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            // TODO: As of KSP v1.0.2, the maxTimeWarping and minTimeWarping parameters behave as time limiters, not actual warp limiters
            int args = CountRemainingArgs(shared);
            double ut;
            switch (args)
            {
                case 1:
                    ut = GetDouble(PopValueAssert(shared));
                    break;

                default:
                    throw new KOSArgumentMismatchException(new[] { 1 }, args);
            }
            AssertArgBottomAndConsume(shared);
            TimeWarp.fetch.WarpTo(ut);
        }
    }
    
}
