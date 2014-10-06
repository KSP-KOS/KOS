namespace kOS.Safe.Encapsulation.Suffixes
{
    public class SetSuffix<T,TR> : Suffix<T,TR>, ISetSuffix
    {
        private readonly SuffixSetDlg<T,TR> setter;

        public SetSuffix(T type, SuffixGetDlg<T,TR> getter, SuffixSetDlg<T,TR> setter, string description = "") : base(type, getter, description)
        {
            this.setter = setter;
        }

        public void Set(object value)
        {
            setter.Invoke(Model, (TR) value);
        }
    }
}