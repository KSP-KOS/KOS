using System;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Safe.Encapsulation
{
    [kOS.Safe.Utilities.KOSNomenclature("Version")]
    public class VersionInfo : Structure
    {
        private readonly int major;
        private readonly int minor;
        private readonly int patch;
        private readonly int build;
        public Version SystemVersion => new Version(major, minor, patch, build);

        public VersionInfo(Version version)
        {
            // NOTICE: there is a clash of nomenclature here.  C# calls the
            // 3rd number "BUILD" and the 4th number "Revision" while the AVC mod
            // (and presumably CKAN) calls the 3rd number "PATCH" and the 4th number "BUILD".
            // We'll be using the AVC terminology in kerboscript, thus why this next line
            // passes in "ver.Revision" where the VersionInfo's "BUILD" goes, and the
            // "ver.Build" where VersionInfo's "PATCH" goes:
            major = version.Major;
            minor = version.Minor;
            patch = version.Build;
            build = version.Revision;
            VersionInitializeSuffixes();
        }
        
        public VersionInfo(int major, int minor, int patch, int build)
        {
            this.major = major;
            this.minor = minor;
            this.patch = patch;
            this.build = build;
            VersionInitializeSuffixes();
        }

        private void VersionInitializeSuffixes()
        {
            AddSuffix("MAJOR", new StaticSuffix<ScalarValue>(() => major));
            AddSuffix("MINOR", new StaticSuffix<ScalarValue>(() => minor));
            AddSuffix("PATCH", new StaticSuffix<ScalarValue>(() => patch));
            AddSuffix("BUILD", new StaticSuffix<ScalarValue>(() => build));
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}.{2}.{3}", major, minor, patch, build);
        }
    }
}
