using System;
using kOS.Safe.Execution;
using kOS.Safe.Encapsulation;

namespace kOS.Safe.Binding
{
    public class BoundVariable : Variable
    {
        public BindingSetDlg Set;
        public BindingGetDlg Get;

        private object currentValue;

        public override object Value
        {
            get
            {
                if (Get != null)
                {
                    if (currentValue == null)
                    {
                        currentValue = Structure.FromPrimitive(Get());
                    }
                    return currentValue;
                }
                return null;
            }
            set
            {
                if (Set == null) return;
                Set(Structure.ToPrimative(value));
            }
        }

        public void ClearCache()
        {
            currentValue = null;
        }
    }
}
