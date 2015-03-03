using System;

namespace kOS.Safe.Encapsulation.Suffixes
{
    public class SetSuffix<TValue> : Suffix<TValue>, ISetSuffix
    {
        private readonly SuffixSetDlg<TValue> setter;

        public SetSuffix(SuffixGetDlg<TValue> getter, SuffixSetDlg<TValue> setter, string description = "")
            : base(getter, description)
        {
            this.setter = setter;
        }

        public virtual void Set(object value)
        {
            TValue toSet;
            if (value is TValue)
            {
                toSet = (TValue) value;
            }
            else
            {
                toSet = (TValue)Convert.ChangeType(value, typeof(TValue));
            }
            setter.Invoke(toSet);
        }
    }
}