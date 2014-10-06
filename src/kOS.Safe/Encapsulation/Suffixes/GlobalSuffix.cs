namespace kOS.Safe.Encapsulation.Suffixes
{

    public class GlobalSuffix<TR> : SuffixBase
    {
        private readonly GlobalSuffixGetDlg<TR> getter;

        public GlobalSuffix(GlobalSuffixGetDlg<TR> getter, string description = "") :base(description)
        {
            this.getter = getter;
        }

        public override object Get()
        {
            return getter.Invoke();
        }
    }
}