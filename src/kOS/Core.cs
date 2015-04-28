using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Persistence;
using kOS.Suffixed.Part;

namespace kOS
{
    public class Core : Structure
    {
        public static VersionInfo VersionInfo;
        private readonly SharedObjects shared;

        static Core()
        {
            VersionInfo = new VersionInfo(0, 17, 1);
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
            AddSuffix("VOLUME", new Suffix<Volume>(() => { throw new NotImplementedException(); }));
        }
    }
}
