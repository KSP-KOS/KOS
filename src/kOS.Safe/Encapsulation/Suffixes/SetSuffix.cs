namespace kOS.Safe.Encapsulation.Suffixes
{
    public class SetSuffix<TParam,TReturn> : Suffix<TParam,TReturn>, ISetSuffix
    {
        private readonly SuffixSetDlg<TParam,TReturn> setter;

        public SetSuffix(TParam type, SuffixGetDlg<TParam,TReturn> getter, SuffixSetDlg<TParam,TReturn> setter, string description = "") : base(type, getter, description)
        {
            this.setter = setter;
        }

        public void Set(object value)
        {
            setter.Invoke(Model, (TReturn) value);
        }
    }
}