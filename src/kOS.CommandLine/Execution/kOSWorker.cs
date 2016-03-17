using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using kOS.CommandLine.Binding;
using kOS.Safe;
using kOS.Safe.Function;
using kOS.Safe.Execution;
using kOS.Safe.Binding;
using kOS.Safe.Compilation;
using kOS.Safe.Module;
using kOS.Safe.Compilation.KS;
using kOS.Safe.Persistence;
using kOS.Safe.Utilities;

namespace kOS.CommandLine.Execution
{
    public class kOSWorker : IProcessor
    {
        private SharedObjects shared;
        public SharedObjects Shared { get { return shared; } }

        ProcessorModes mode = ProcessorModes.READY;

        public kOSWorker()
        {
            SafeHouse.Init(Encapsulation.Config.Fetch, null, null, true, Path.Combine(Directory.GetCurrentDirectory(), "Scripts"));
            SafeHouse.Logger = new ConsoleLogger();
            //SafeHouse.ArchiveFolder = System.IO.Directory.GetCurrentDirectory();
            Init();
        }

        public void Init()
        {
            Opcode.InitMachineCodeData();
            CompiledObject.InitTypeData();
            AssemblyWalkAttribute.Walk();

            shared = new SharedObjects();
            shared.Processor = this;
            shared.UpdateHandler = new UpdateHandler();
            shared.BindingMgr = new BindingManager(shared);
            //shared.Interpreter = new Screen.Interpreter();
            shared.Screen = shared.Interpreter;
            shared.ScriptHandler = new KSScript();
            shared.Logger = SafeHouse.Logger;
            shared.VolumeMgr = new VolumeManager();
            shared.FunctionManager = new FunctionManager(shared);
            shared.Cpu = new CPU(shared);
            //shared.SoundMaker = Sound.SoundMaker.Instance;

            // add archive.
            var archive = new Archive();
            shared.VolumeMgr.Add(archive);
            shared.VolumeMgr.SwitchTo(archive);

            shared.Cpu.Boot();
        }

        public void PushScript(string content)
        {
            if (shared != null && shared.ScriptHandler != null)
            {
                string filepath = "<sys:push>";
                var options = new CompilerOptions
                {
                    LoadProgramsInSameAddressSpace = false,
                    FuncManager = shared.FunctionManager,
                    IsCalledFromRun = false
                };
                //var program = shared.Cpu.SwitchToProgramContext();
                var program = shared.Cpu.GetInterpreterContext();
                var parts = shared.ScriptHandler.Compile(filepath, 0, content, "interpreter", options);
                var instruction = program.AddObjectParts(parts);
                shared.Cpu.InstructionPointer++;
            }
        }

        public void FixedUpdate()
        {
            shared.UpdateHandler.UpdateFixedObservers(0.04);
        }

        public void SetMode(ProcessorModes newProcessorMode)
        {
            mode = newProcessorMode;
        }

        public ProcessorModes GetMode()
        {
            return mode;
        }

        public string BootFilename
        {
            get
            {
                // TODO: implement using a boot file.
                return "None";
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public bool CheckCanBoot()
        {
            // TODO: Check if we can boot.
            return true;
        }
    }
}
