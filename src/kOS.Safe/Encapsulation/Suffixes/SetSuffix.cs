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
            System.Console.WriteLine("eraseme: SetSuffix.Set was passed a "+value.GetType().ToString()+" with value = "+value.ToString()+" when looking for a thing of type "+typeof(TValue).ToString());
            
            TValue toSet;
            if (value is TValue)
            {
                toSet = (TValue) value;
            }
            else
            {
                Structure newValue = Structure.FromPrimitiveWithAssert(value);  // Handles the string -> StringValue case that Convert.ChangeType() can't.
                System.Console.WriteLine("eraseme: SetSuffix.Set newValue = "+newValue.GetType()+" "+newValue.ToString());
                toSet = (TValue)Convert.ChangeType(newValue, typeof(TValue));
                System.Console.WriteLine("eraseme: SetSuffix.Set toSet = "+toSet.GetType()+" "+toSet.ToString());
            }
            setter.Invoke(toSet);
        }
    }
}