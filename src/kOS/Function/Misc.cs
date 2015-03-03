using System;
using System.Collections.Generic;
using FinePrint.Utilities;
using kOS.Execution;
using kOS.Safe.Compilation;
using kOS.Safe.Exceptions;
using kOS.Safe.Function;
using kOS.Safe.Module;
using kOS.Safe.Persistence;
using kOS.Suffixed;
using kOS.Utilities;
using kOS.Safe.Utilities;

namespace kOS.Function
{
    [Function("clearscreen")]
    public class FunctionClearScreen : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            shared.Window.ClearScreen();
        }
    }

    [Function("print")]
    public class FunctionPrint : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string textToPrint = shared.Cpu.PopValue().ToString();
            shared.Screen.Print(textToPrint);
        }
    }
    
    [Function("hudtext")]
    public class FunctionHudText : FunctionBase
    {
        public override void Execute (SharedObjects shared)

        {
            bool      echo      = Convert.ToBoolean(shared.Cpu.PopValue());
            RgbaColor rgba      = GetRgba(shared.Cpu.PopValue());
            int       size      = Convert.ToInt32 (shared.Cpu.PopValue ());    
            int       style     = Convert.ToInt32 (shared.Cpu.PopValue ());
            int       delay     = Convert.ToInt32 (shared.Cpu.PopValue ());   
            string    textToHud = shared.Cpu.PopValue ().ToString ();
            string   htmlColour = rgba.ToHexNotation();
            switch (style)
            {
                case 1:
                    ScreenMessages.PostScreenMessage("<color=" + htmlColour + "><size=" + size + ">" + textToHud + "</size></color>",delay,ScreenMessageStyle.UPPER_LEFT);
                    break;
                case 2:
                    ScreenMessages.PostScreenMessage("<color=" + htmlColour + "><size=" + size + ">" + textToHud + "</size></color>",delay,ScreenMessageStyle.UPPER_CENTER);
                    break;
                case 3:
                    ScreenMessages.PostScreenMessage("<color=" + htmlColour + "><size=" + size + ">" + textToHud + "</size></color>",delay,ScreenMessageStyle.UPPER_RIGHT);
                    break;
                case 4:
                    ScreenMessages.PostScreenMessage("<color=" + htmlColour + "><size=" + size + ">" + textToHud + "</size></color>",delay,ScreenMessageStyle.LOWER_CENTER);
                    break;
                default:
                    ScreenMessages.PostScreenMessage("*" + textToHud, 3f, ScreenMessageStyle.UPPER_CENTER);
                    break;
            }
            if (echo) {
                shared.Screen.Print ("HUD: " + textToHud);
            }
        }
    }
    
    [Function("printat")]
    public class FunctionPrintAt : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            int row = Convert.ToInt32(shared.Cpu.PopValue());
            int column = Convert.ToInt32(shared.Cpu.PopValue());
            string textToPrint = shared.Cpu.PopValue().ToString();
            shared.Screen.PrintAt(textToPrint, row, column);
        }
    }

    [Function("toggleflybywire")]
    public class FunctionToggleFlyByWire : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            bool enabled = Convert.ToBoolean(shared.Cpu.PopValue());
            string paramName = shared.Cpu.PopValue().ToString();
            ((CPU)shared.Cpu).ToggleFlyByWire(paramName, enabled);
            // Work around to prevent the pop error following toggle fly by wire directly. 
            // The VisitIdentifierLedExpression method in the Compiler class purposfully throws away the returned value of a function.
            ((CPU)shared.Cpu).PushStack(0);

        }
    }

    [Function("selectautopilotmode")]
    public class FunctionSelectAutopilotMode : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string autopilotMode = shared.Cpu.PopValue().ToString();
            ((CPU)shared.Cpu).SelectAutopilotMode(autopilotMode);
            // The VisitIdentifierLedExpression method in the Compiler class purposfully throws away the returned value of a function.
            ((CPU)shared.Cpu).PushStack(0);

        }
    }

    [Function("stage")]
    public class FunctionStage : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
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
                throw new KOSCommandInvalidHere("STAGE", "a non-active SHIP, KSP does not support this", "Core is on the active vessel");
            }
        }
    }

    [Function("run")]
    public class FunctionRun : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            object volumeId = shared.Cpu.PopValue(true);
            string fileName = shared.Cpu.PopValue(true).ToString();
            
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
                        string filePath = string.Format("{0}/{1}", shared.VolumeMgr.GetVolumeRawIdentifier(targetVolume), fileName) ;
                        List<CodePart> parts = shared.ScriptHandler.Compile(filePath, 1, file.StringContent);
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
                string filePath = shared.VolumeMgr.GetVolumeRawIdentifier(shared.VolumeMgr.CurrentVolume) + "/" + fileName ;
                var options = new CompilerOptions {LoadProgramsInSameAddressSpace = true};
                var programContext = ((CPU)shared.Cpu).GetProgramContext();

                List<CodePart> codeParts;
                if (file.Category == FileCategory.KSM)
                {
                    string prefix = programContext.Program.Count.ToString();
                    codeParts = shared.VolumeMgr.CurrentVolume.LoadObjectFile(filePath, prefix, file.BinaryContent);
                }
                else
                {
                    codeParts = shared.ScriptHandler.Compile(filePath, 1, file.StringContent, "program", options);
                }
                programContext.AddParts(codeParts);                
            }
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
            object topStack = shared.Cpu.PopValue(true); // null if there's no output file (output file means compile, not run).
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
            topStack = shared.Cpu.PopValue(true);
            if (topStack != null)
                fileName = topStack.ToString();

            if (fileName == null)
                throw new KOSFileException("No filename to load was given.");
            
            ProgramFile file = shared.VolumeMgr.CurrentVolume.GetByName(fileName, (! justCompiling)); // if running, look for KSM first.  If compiling look for KS first.
            fileName = file.Filename; // just in case GetByName picked an extension that changed it.

            // filename is now guaranteed to have an extension.  To make default output name, replace the extension with KSM:
            if (defaultOutput)
                fileNameOut = fileName.Substring(0, fileName.LastIndexOf('.')) + "." + Volume.KOS_MACHINELANGUAGE_EXTENSION;

            if (fileNameOut != null && fileName == fileNameOut)
                throw new KOSFileException("Input and output filenames must differ.");

            if (shared.VolumeMgr == null) return;
            if (shared.VolumeMgr.CurrentVolume == null) throw new KOSFileException("Volume not found");

            if (file == null) throw new KOSFileException(string.Format("File '{0}' not found", fileName));

            if (shared.ScriptHandler != null)
            {
                var options = new CompilerOptions { LoadProgramsInSameAddressSpace = true };
                string filePath = shared.VolumeMgr.GetVolumeRawIdentifier(shared.VolumeMgr.CurrentVolume) + "/" + fileName;
                // add this program to the address space of the parent program,
                // or to a file to save:
                if (justCompiling)
                {
                    List<CodePart> compileParts = shared.ScriptHandler.Compile(filePath, 1, file.StringContent, String.Empty, options);
                    shared.VolumeMgr.CurrentVolume.SaveObjectFile(fileNameOut, compileParts);
                }
                else
                {
                    var programContext = ((CPU)shared.Cpu).GetProgramContext();
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
            var node = (Node)shared.Cpu.PopValue();
            node.AddToVessel(shared.Vessel);
        }
    }

    [Function("remove")]
    public class FunctionRemoveNode : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var node = (Node)shared.Cpu.PopValue();
            node.Remove();
        }
    }

    [Function("logfile")]
    public class FunctionLogFile : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string fileName = shared.Cpu.PopValue(true).ToString();
            string expressionResult = shared.Cpu.PopValue().ToString();

            if (shared.VolumeMgr != null)
            {
                Volume volume = shared.VolumeMgr.CurrentVolume;
                if (volume != null)
                {
                    volume.AppendToFile(fileName, expressionResult);
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
                shared.Processor.SetMode(ProcessorModes.OFF);
                shared.Processor.SetMode(ProcessorModes.READY);
            }
        }
    }

    [Function("shutdown")]
    public class FunctionShutdown : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            if (shared.Processor != null) shared.Processor.SetMode(ProcessorModes.OFF);
        }
    }

    [Function("debugglobalvars")]
    public class DebugGlobalVars : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            shared.Cpu.DumpVariables();
        }
    }

}
