﻿using System;
using System.Collections.Generic;
using kOS.Persistence;
using kOS.Module;
using kOS.Suffixed;
using kOS.Compilation;

namespace kOS.Function
{
    [FunctionAttribute("clearscreen")]
    public class FunctionClearScreen : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            shared.Screen.ClearScreen();
        }
    }

    [FunctionAttribute("print")]
    public class FunctionPrint : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string textToPrint = shared.Cpu.PopValue().ToString();
            shared.Screen.Print(textToPrint);
        }
    }

    [FunctionAttribute("printat")]
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

    [FunctionAttribute("toggleflybywire")]
    public class FunctionToggleFlyByWire : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            bool enabled = Convert.ToBoolean(shared.Cpu.PopValue());
            string paramName = shared.Cpu.PopValue().ToString();
            shared.Cpu.ToggleFlyByWire(paramName, enabled);
        }
    }

    [FunctionAttribute("stage")]
    public class FunctionStage : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            Staging.ActivateNextStage();
        }
    }

    [FunctionAttribute("run")]
    public class FunctionRun : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            object volumeId = shared.Cpu.PopValue();
            string fileName = shared.Cpu.PopValue().ToString();
            
            if (shared.VolumeMgr == null) return;
            if (shared.VolumeMgr.CurrentVolume == null) throw new Exception("Volume not found");

            ProgramFile file = shared.VolumeMgr.CurrentVolume.GetByName(fileName);
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
                    throw new Exception("Volume not found");
                }
            }
            else
            {
                // clear the "program" compilation context
                shared.ScriptHandler.ClearContext("program");
                string filePath = shared.VolumeMgr.GetVolumeRawIdentifier(shared.VolumeMgr.CurrentVolume) + "/" + fileName ;
                var options = new CompilerOptions {LoadProgramsInSameAddressSpace = true};
                List<CodePart> parts;
                var programContext = shared.Cpu.GetProgramContext();
                if (file.Category == FileCategory.KEXE)
                {
                    string prefix = programContext.Program.Count.ToString();
                    parts = shared.VolumeMgr.CurrentVolume.LoadObjectFile(filePath, 1, prefix, file.BinaryContent);
                }
                else
                    parts = shared.ScriptHandler.Compile(filePath, 1, file.StringContent, "program", options);
                programContext.AddParts(parts);
                
                string erasemeString = Utilities.Utils.GetCodeFragment(programContext.Program);  // eraaseme - remove after debugging is done.
                UnityEngine.Debug.Log("(PROGRAM DUMP OF " + filePath + ")\n"+erasemeString);     // eraaseme - remove after debugging is done.
            }
        }
    }

    [FunctionAttribute("load")]
    public class FunctionLoad : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string fileNameOut = null;
            object topStack = shared.Cpu.PopValue(); // null if there's no output file (output file means compile, not run).
            if (topStack!=null)
                fileNameOut = topStack.ToString();

            string fileName = null;
            topStack = shared.Cpu.PopValue(); // null if there's no output file (output file means compile, not run).
            if (topStack!=null)
                fileName = topStack.ToString();

            if (fileName != null && fileNameOut != null && fileName == fileNameOut)
                throw new Exception("Input and output filenames must differ.");

            if (shared.VolumeMgr == null) return;
            if (shared.VolumeMgr.CurrentVolume == null) throw new Exception("Volume not found");

            ProgramFile file = shared.VolumeMgr.CurrentVolume.GetByName(fileName);
            if (file == null) throw new Exception(string.Format("File '{0}' not found", fileName));

            if (shared.ScriptHandler != null)
            {
                var options = new CompilerOptions {LoadProgramsInSameAddressSpace = true};
                string filePath = shared.VolumeMgr.GetVolumeRawIdentifier(shared.VolumeMgr.CurrentVolume) + "/" + fileName ;
                // add this program to the address space of the parent program,
                // or to a file to save:
                if (fileNameOut != null)
                {
                    List<CodePart> compileParts = shared.ScriptHandler.Compile(filePath, 1, file.StringContent, String.Empty, options);
                    shared.VolumeMgr.CurrentVolume.SaveObjectFile(fileNameOut,compileParts);
                }
                else
                {
                    var programContext = shared.Cpu.GetProgramContext();
                    List<CodePart> parts;
                    if (file.Category == FileCategory.KEXE)
                    {
                        string prefix = programContext.Program.Count.ToString();
                        parts = shared.VolumeMgr.CurrentVolume.LoadObjectFile(filePath, 1, prefix, file.BinaryContent);
                    }
                    else
                        parts = shared.ScriptHandler.Compile(filePath, 1, file.StringContent, "program", options);
                    int programAddress = programContext.AddObjectParts(parts);
                    // push the entry point address of the new program onto the stack
                    shared.Cpu.PushStack(programAddress);                    
                }
            }
        }
    }

    [FunctionAttribute("add")]
    public class FunctionAddNode : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var node = (Node)shared.Cpu.PopValue();
            node.AddToVessel(shared.Vessel);
        }
    }

    [FunctionAttribute("remove")]
    public class FunctionRemoveNode : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var node = (Node)shared.Cpu.PopValue();
            node.Remove();
        }
    }

    [FunctionAttribute("logfile")]
    public class FunctionLogFile : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string fileName = shared.Cpu.PopValue().ToString();
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
                    throw new Exception("Volume not found");
                }
            }
        }
    }

    [FunctionAttribute("reboot")]
    public class FunctionReboot : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            if (shared.Cpu != null) shared.Cpu.Boot();
        }
    }

    [FunctionAttribute("shutdown")]
    public class FunctionShutdown : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            if (shared.Processor != null) shared.Processor.SetMode(kOSProcessor.Modes.OFF);
        }
    }
}
