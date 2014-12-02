namespace kOS.Safe.Encapsulation.Suffixes
{

    public class StaticSuffix<TReturn> : SuffixBase
    {
        private readonly StaticSuffixGetDlg<TReturn> getter;

        public StaticSuffix(StaticSuffixGetDlg<TReturn> getter, string description = "") :base(description)
        {
            this.getter = getter;
        }

        public override object Get()
        {
            return getter.Invoke();
        }
    }
}