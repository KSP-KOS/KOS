using kOS.Safe.Utilities;

namespace kOS.Safe.Encapsulation.Suffixes
{
    public class Suffix<TReturn> : SuffixBase
    {
        private readonly SuffixGetDlg<TReturn> getter;

        public Suffix(SuffixGetDlg<TReturn> getter, string description = ""):base(description)
        {
            this.getter = getter;
        }

        public override object Get()
        {
            return getter.Invoke();
        }
    }

}
