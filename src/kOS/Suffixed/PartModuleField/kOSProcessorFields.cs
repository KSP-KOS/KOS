using kOS.Safe.Encapsulation.Suffixes;
using kOS.Module;
using kOS.Safe.Encapsulation;
using kOS.Safe.Persistence;

namespace kOS.Suffixed.PartModuleField
{
    public class kOSProcessorFields : PartModuleFields
    {
        private readonly kOSProcessor processor;

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
            AddSuffix("TAG", new NoArgsSuffix<StringValue>(() => processor.Tag, "This processor's tag"));
            AddSuffix("BOOTFILENAME", new SetSuffix<StringValue>(GetBootFilename, SetBootFilename, "The name of the processor's boot file."));
        }

        private void Activate()
        {
            ThrowIfNotCPUVessel();

            processor.ProcessorMode = kOS.Safe.Module.ProcessorModes.STARVED;
        }

        private void Deactivate()
        {
            ThrowIfNotCPUVessel();

            processor.ProcessorMode = kOS.Safe.Module.ProcessorModes.OFF;
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
