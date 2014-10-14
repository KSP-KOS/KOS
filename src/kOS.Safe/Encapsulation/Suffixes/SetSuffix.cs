using System;

namespace kOS.Safe.Encapsulation.Suffixes
{
    public class SetSuffix<TParam,TValue> : Suffix<TParam,TValue>, ISetSuffix
    {
        private readonly SuffixSetDlg<TParam,TValue> setter;

        public SetSuffix(TParam type, SuffixGetDlg<TParam,TValue> getter, SuffixSetDlg<TParam,TValue> setter, string description = "") : base(type, getter, description)
        {
            this.setter = setter;
        }

        public void Set(object value)
        {
            var test = (TValue)Convert.ChangeType(value, typeof(TValue));
            setter.Invoke(Model, test);
        }
    }
}