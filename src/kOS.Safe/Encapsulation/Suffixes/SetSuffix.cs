using System;

namespace kOS.Safe.Encapsulation.Suffixes
{
    public class SetSuffix<TValue> : Suffix<TValue>, ISetSuffix where TValue : Structure
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
                Structure newValue = Structure.FromPrimitiveWithAssert(value);  // Handles converting built in types to Structures that Convert.ChangeType() can't.
                toSet = (TValue)Convert.ChangeType(newValue, typeof(TValue));
            }
            setter.Invoke(toSet);
        }
    }
}