using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Persistence;
using kOS.Safe.Utilities;
using kOS.Module;
using kOS.Suffixed;
using kOS.Suffixed.Part;
using kOS.Suffixed.PartModuleField;
using kOS.Utilities;
using System.Linq;
using kOS.Communication;

namespace kOS
{
    [kOS.Safe.Utilities.KOSNomenclature("Core")]
    public class Core : kOSProcessorFields
    {
        public static VersionInfo VersionInfo;

        static Core()
        {
            var ver = typeof(Core).Assembly.GetName().Version;
            VersionInfo = new VersionInfo(ver.Major, ver.Minor, ver.Build);
        }

        public Core(kOSProcessor processor, SharedObjects shared):base(processor, shared)
        {
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("VERSION", new Suffix<VersionInfo>(() => VersionInfo));
            AddSuffix("VESSEL", new Suffix<VesselTarget>(() => VesselTarget.CreateOrGetExisting(shared.KSPPart.vessel, shared)));
            AddSuffix("ELEMENT", new Suffix<ElementValue>(GetEelement));
            AddSuffix("CURRENTVOLUME", new Suffix<Volume>(GetCurrentVolume, "The currently selected volume"));
            AddSuffix("MESSAGES", new NoArgsSuffix<MessageQueueStructure>(() => new MessageQueueStructure(processor.Messages, shared),
                "This processor's message queue"));
        }

        private ElementValue GetEelement()
        {
            var elList = shared.KSPPart.vessel.PartList("elements", shared);
            var part = new PartValue(shared.KSPPart, shared);
            return elList.Cast<ElementValue>().FirstOrDefault(el => el.Parts.Contains(part));
        }

        private Volume GetCurrentVolume()
        {
            return shared.VolumeMgr.CurrentVolume;
        }
    }
}