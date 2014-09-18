using System;
using System.Collections.Generic;
using kOS.Module;
using kOS.Persistence;
using kOS.Compilation;

namespace kOS.InterProcessor
{
    public class ProcessorManager
    {
        // Use the attached volume as processor identifier
        private readonly Dictionary<Volume, kOSProcessor> processors;

        public ProcessorManager()
        {
            processors = new Dictionary<Volume, kOSProcessor>();
        }

        public void UpdateProcessors(List<kOSProcessor> processorList)
        {
            processors.Clear();
            foreach (kOSProcessor processor in processorList)
            {
                processors.Add(processor.HardDisk, processor);
            }
        }

        private kOSProcessor GetProcessor(Volume volume)
        {
            if (processors.ContainsKey(volume))
            {
                return processors[volume];
            }
            throw new Exception("The volume is not attached to any processor");
        }

        public void RunProgramOn(List<Opcode> program, Volume volume)
        {
            kOSProcessor processor = GetProcessor(volume);
            var runCommand = new RunCommand {Program = program};
            processor.ExecuteInterProcCommand(runCommand);            
        }
    }
}
