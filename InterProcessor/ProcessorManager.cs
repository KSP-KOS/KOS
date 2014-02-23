using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.InterProcessor
{
    public class ProcessorManager
    {
        private SharedObjects _shared;
        // Use the attached volume as processor identifier
        private Dictionary<Volume, kOSProcessor> _processors;

        public ProcessorManager(SharedObjects shared)
        {
            _shared = shared;
            _processors = new Dictionary<Volume, kOSProcessor>();
        }

        public void UpdateProcessors(List<kOSProcessor> processorList)
        {
            _processors.Clear();
            foreach (kOSProcessor processor in processorList)
            {
                _processors.Add(processor.hardDisk, processor);
            }
        }

        private kOSProcessor GetProcessor(Volume volume)
        {
            if (_processors.ContainsKey(volume))
            {
                return _processors[volume];
            }
            else
            {
                throw new Exception("The volume is not attached to any processor");
            }
        }

        public void RunProgramOn(List<Opcode> program, Volume volume)
        {
            kOSProcessor processor = GetProcessor(volume);
            RunCommand runCommand = new RunCommand();
            runCommand.Program = program;
            processor.ExecuteInterProcCommand(runCommand);            
        }
    }
}
