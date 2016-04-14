using System;
using kOS.Safe.Encapsulation;
using kOS.Module;
using kOS.Safe.Exceptions;
using kOS.Suffixed.PartModuleField;
using kOS.Safe.Communication;

namespace kOS.Communication
{
    [kOS.Safe.Utilities.KOSNomenclature("Connection", KOSToCSharp = false)]
    public class ProcessorConnection : Connection<kOS.SharedObjects>
    {
        private kOSProcessor processor;

        public override bool Connected {
            get {
                return IsCpuVessel();
            }
        }

        public override double Delay {
            get {
                return IsCpuVessel() ? 0 : -1;
            }
        }

        public ProcessorConnection(kOSProcessor processor, SharedObjects shared) : base(shared)
        {
            this.processor = processor;
        }

        protected override BooleanValue SendMessage(Structure content)
        {
            if (!Connected)
            {
                return false;
            }

            processor.Send(content);

            return true;
        }

        public override string ToString()
        {
            return "PROCESSOR CONNECTION(" + processor.Tag + ")";
        }

        public void ThrowIfNotCPUVessel()
        {
            if (!IsCpuVessel())
                throw new KOSWrongCPUVesselException();
        }

        public bool IsCpuVessel()
        {
            return processor.vessel.id == shared.Vessel.id;
        }

        protected override Structure Destination()
        {
            return new kOSProcessorFields(processor, shared);
        }
    }
}

