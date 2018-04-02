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
            // NOTICE: there is a clash of nomenclature here.  C# calls the
            // 3rd number "BUILD" and the 4th number "Revision" while the AVC mod
            // (and presumably CKAN) calls the 3rd number "PATCH" and the 4th number "BUILD".
            // We'll be using the AVC terminology in kerboscript, thus why this next line
            // passes in "ver.Revision" where the VersionInfo's "BUILD" goes, and the
            // "ver.Build" where VersionInfo's "PATCH" goes:
            VersionInfo = new VersionInfo(ver.Major, ver.Minor, ver.Build, ver.Revision);
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
            var part = VesselTarget.CreateOrGetExisting(shared)[shared.KSPPart];
            return elList.Cast<ElementValue>().FirstOrDefault(el => el.Parts.Contains(part));
        }

        private Volume GetCurrentVolume()
        {
            return shared.VolumeMgr.CurrentVolume;
        }
    }
}
