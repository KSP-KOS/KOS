using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Persistence;
using kOS.Suffixed.Part;
using kOS.Suffixed;
using kOS.Utilities;
using System.Linq;

namespace kOS
{
    public class Core : Structure
    {
        public static VersionInfo VersionInfo;
        private readonly SharedObjects shared;

        static Core()
        {
            var ver = typeof(Core).Assembly.GetName().Version;
            VersionInfo = new VersionInfo(ver.Major, ver.Minor, ver.Build);
        }

        public Core(SharedObjects shared)
        {
            this.shared = shared;
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("VERSION", new Suffix<VersionInfo>(() => VersionInfo));
            AddSuffix("PART", new Suffix<PartValue>(() => new PartValue(shared.KSPPart, shared)));
            AddSuffix("VESSEL", new Suffix<VesselTarget>(() => new VesselTarget(shared.KSPPart.vessel, shared)));
            AddSuffix("ELEMENT", new Suffix<ElementValue>(getEelement));
            AddSuffix("VOLUME", new Suffix<Volume>(() => { throw new NotImplementedException(); }));
        }

        private ElementValue getEelement()
        {
            var elList = shared.KSPPart.vessel.PartList("elements", shared);
            var part = new PartValue(shared.KSPPart, shared);
            foreach (ElementValue el in elList)
            {
                if (el.Parts.Contains(part)) return el;
            }
            return null;
        }
    }
}
