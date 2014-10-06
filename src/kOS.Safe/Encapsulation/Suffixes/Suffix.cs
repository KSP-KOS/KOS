namespace kOS.Safe.Encapsulation.Suffixes
{
    public class Suffix<T,TR> : SuffixBase
    {
        protected T Model { get; private set; }
        private readonly SuffixGetDlg<T,TR> getter;

        public Suffix(T type, SuffixGetDlg<T,TR> getter, string description = ""):base(description)
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
