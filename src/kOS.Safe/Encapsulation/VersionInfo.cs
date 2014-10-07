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
        }

        protected override void InitializeSuffixes()
        {
            AddSuffix("MAJOR", new GlobalSuffix<int>(() => major));
            AddSuffix("MINOR", new GlobalSuffix<int>(() => minor));
            AddSuffix("BUILD", new GlobalSuffix<int>(() => build));
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}.{2}", major, minor, build);
        }
    }
}
