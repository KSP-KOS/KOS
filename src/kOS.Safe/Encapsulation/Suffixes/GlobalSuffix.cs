namespace kOS.Safe.Encapsulation.Suffixes
{

    public class StaticSuffix<TReturn> : SuffixBase where TReturn : Structure
    {
        private readonly StaticSuffixGetDlg<TReturn> getter;

        public StaticSuffix(StaticSuffixGetDlg<TReturn> getter, string description = "") :base(description)
        {
            this.getter = getter;
        }

        public override ISuffixResult Get()
        {
            return new SuffixResult(getter.Invoke());
        }
    }
}