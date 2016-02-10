namespace kOS.Safe.Encapsulation.Suffixes
{
    public class Suffix<TReturn> : SuffixBase where TReturn : Structure
    {
        private readonly SuffixGetDlg<TReturn> getter;

        public Suffix(SuffixGetDlg<TReturn> getter, string description = ""):base(description)
        {
            this.getter = getter;
        }

        public override ISuffixResult Get()
        {
            return new SuffixResult(getter.Invoke());
        }
    }
}
