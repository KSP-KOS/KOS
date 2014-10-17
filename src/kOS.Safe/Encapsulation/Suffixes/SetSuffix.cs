using System;

namespace kOS.Safe.Encapsulation.Suffixes
{
    public class SetSuffix<TValue> : Suffix<TValue>, ISetSuffix
    {
        private readonly SuffixSetDlg<TValue> setter;

        public SetSuffix(SuffixGetDlg<TValue> getter, SuffixSetDlg<TValue> setter, string description = "") : base(getter, description)
        {
            this.setter = setter;
        }

        public void Set(object value)
        {
            var test = (TValue)Convert.ChangeType(value, typeof(TValue));
            setter.Invoke(test);
        }
    }
}