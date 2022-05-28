using kOS.Safe.Encapsulation.Suffixes;
using kOS.Module;
using kOS.Safe.Encapsulation;
using kOS.Safe.Persistence;
using kOS.Safe.Module;
using System;
using kOS.Communication;

namespace kOS.Suffixed.PartModuleField
{
    [kOS.Safe.Utilities.KOSNomenclature("KOSProcessor")]
    public class kOSProcessorFields : PartModuleFields
    {
        protected readonly kOSProcessor processor;

        public kOSProcessorFields(kOSProcessor processor, SharedObjects sharedObj):base(processor, sharedObj)
        {
            this.processor = processor;
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("MODE", new NoArgsSuffix<StringValue>(() => processor.ProcessorMode.ToString(), "This processor's mode"));
            AddSuffix("ACTIVATE", new NoArgsVoidSuffix(Activate, "Activate this processor"));
            AddSuffix("DEACTIVATE", new NoArgsVoidSuffix(Deactivate, "Deactivate this processor"));
            AddSuffix("VOLUME", new NoArgsSuffix<Volume>(() => processor.HardDisk, "This processor's hard disk"));
            AddSuffix("TAG", new SetSuffix<StringValue>(() => processor.Tag, value => processor.Tag = value, "This processor's tag name"));
            AddSuffix("BOOTFILENAME", new SetSuffix<StringValue>(GetBootFilename, SetBootFilename, "The name of the processor's boot file."));
            AddSuffix("CONNECTION", new NoArgsSuffix<ProcessorConnection>(() => new ProcessorConnection(processor, shared), "Get a connection to this processor"));
        }

        private void Activate()
        {
            ThrowIfNotCPUVessel();

            processor.SetMode(ProcessorModes.STARVED);
        }

        private void Deactivate()
        {
            ThrowIfNotCPUVessel();

            processor.SetMode(ProcessorModes.OFF);
        }

        private StringValue GetBootFilename()
        {
            return processor.BootFilename;
        }

        private void SetBootFilename(StringValue name)
        {
            ThrowIfNotCPUVessel();

            processor.BootFilename = name;
        }
    }
}
