namespace kOS.Safe.Encapsulation
{
    public class Suffix<T,TR> : ISuffix
    {
        protected T Model { get; private set; }
        private readonly SuffixGetDlg<T,TR> getter;

        public Suffix(T type, SuffixGetDlg<T,TR> getter)
        {
            Model = type;
            this.getter = getter;
        }

        public object Get()
        {
            return getter.Invoke(Model);
        }

        public string Description { get; set; }
    }

}
