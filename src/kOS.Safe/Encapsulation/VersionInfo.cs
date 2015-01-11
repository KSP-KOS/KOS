using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Safe.Encapsulation
{
    public class VersionInfo : Structure
    {
        private readonly int major;
        private readonly int minor;
        private readonly int build;

        public VersionInfo(int major, int minor, int build)
        {
            this.major = major;
            this.minor = minor;
            this.build = build;
            VersionInitializeSuffixes();
        }

        private void VersionInitializeSuffixes()
        {
            AddSuffix("MAJOR", new StaticSuffix<int>(() => major));
            AddSuffix("MINOR", new StaticSuffix<int>(() => minor));
            AddSuffix("BUILD", new StaticSuffix<int>(() => build));
        }

        public override bool KOSEquals(object other)
        {
            VersionInfo otherVInfo = other as VersionInfo;
            if (otherVInfo == null) return false;
            return this.major == otherVInfo.major && this.minor == otherVInfo.minor && this.build == otherVInfo.build;
        } 

        public override string ToString()
        {
            return string.Format("{0}.{1}.{2}", major, minor, build);
        }
    }
}
