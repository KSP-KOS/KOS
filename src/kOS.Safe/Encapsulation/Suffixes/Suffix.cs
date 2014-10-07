namespace kOS.Safe.Encapsulation.Suffixes
{
    public class Suffix<T,TReturn> : SuffixBase
    {
        protected T Model { get; private set; }
        private readonly SuffixGetDlg<T,TReturn> getter;

        public Suffix(T type, SuffixGetDlg<T,TReturn> getter, string description = ""):base(description)
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
