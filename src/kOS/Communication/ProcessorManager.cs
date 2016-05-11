using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Module;
using kOS.Safe.Compilation;
using kOS.Safe.Persistence;
using kOS.Safe.Utilities;

namespace kOS.Communication
{
    public class ProcessorManager
    {
        // Use the attached volume as processor identifier
        public Dictionary<Volume, kOSProcessor> processors { get; private set; }

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

        public kOSProcessor GetProcessor(string name)
        {
            foreach (KeyValuePair<Volume, kOSProcessor> pair in processors)
            {
                if (pair.Value.Tag != null && String.Equals(pair.Value.Tag, name, StringComparison.CurrentCultureIgnoreCase))
                {
                    return pair.Value;
                }
            }

            return null;
        }

        public kOSProcessor GetProcessor(Volume volume)
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
