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
            AddSuffix("ELEMENT", new Suffix<ElementValue>(GetEelement));
            AddSuffix("VOLUME", new Suffix<Volume>(() => { throw new NotImplementedException(); }));
        }

        private ElementValue GetEelement()
        {
            var elList = shared.KSPPart.vessel.PartList("elements", shared);
            var part = new PartValue(shared.KSPPart, shared);
            return elList.Cast<ElementValue>().FirstOrDefault(el => el.Parts.Contains(part));
        }
    }
}
