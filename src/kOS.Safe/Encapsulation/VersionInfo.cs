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
            AddSuffix("MAJOR", new StaticSuffix<ScalarValue>(() => major));
            AddSuffix("MINOR", new StaticSuffix<ScalarValue>(() => minor));
            AddSuffix("BUILD", new StaticSuffix<ScalarValue>(() => build));
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}.{2}", major, minor, build);
        }
    }
}
