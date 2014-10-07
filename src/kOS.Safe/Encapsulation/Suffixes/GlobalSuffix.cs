namespace kOS.Safe.Encapsulation.Suffixes
{

    public class GlobalSuffix<TReturn> : SuffixBase
    {
        private readonly GlobalSuffixGetDlg<TReturn> getter;

        public GlobalSuffix(GlobalSuffixGetDlg<TReturn> getter, string description = "") :base(description)
        {
            this.getter = getter;
        }

        public override object Get()
        {
            return getter.Invoke();
        }
    }
}