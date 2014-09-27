namespace kOS.Safe.Encapsulation
{
    public class SetSuffix<T,TR> : Suffix<T,TR>, ISetSuffix
    {
        private readonly SuffixSetDlg<T,TR> setter;

        public SetSuffix(T type, SuffixGetDlg<T,TR> getter, SuffixSetDlg<T,TR> setter) : base(type, getter)
        {
            this.setter = setter;
        }

        public void Set(object value)
        {
            setter.Invoke(Model, (TR) value);
        }
    }
}