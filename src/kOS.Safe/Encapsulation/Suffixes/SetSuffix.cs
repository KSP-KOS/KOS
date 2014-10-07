namespace kOS.Safe.Encapsulation.Suffixes
{
    public class SetSuffix<T,TReturn> : Suffix<T,TReturn>, ISetSuffix
    {
        private readonly SuffixSetDlg<T,TReturn> setter;

        public SetSuffix(T type, SuffixGetDlg<T,TReturn> getter, SuffixSetDlg<T,TReturn> setter, string description = "") : base(type, getter, description)
        {
            this.setter = setter;
        }

        public void Set(object value)
        {
            setter.Invoke(Model, (TReturn) value);
        }
    }
}