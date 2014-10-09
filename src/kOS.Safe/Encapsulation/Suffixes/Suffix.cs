namespace kOS.Safe.Encapsulation.Suffixes
{
    public class Suffix<TParam,TReturn> : SuffixBase
    {
        protected TParam Model { get; private set; }
        private readonly SuffixGetDlg<TParam,TReturn> getter;

        public Suffix(TParam type, SuffixGetDlg<TParam,TReturn> getter, string description = ""):base(description)
        {
            Model = type;
            this.getter = getter;
        }

        public override object Get()
        {
            return getter.Invoke(Model);
        }
    }

}
