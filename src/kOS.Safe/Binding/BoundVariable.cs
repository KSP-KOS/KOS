using System;
using kOS.Safe.Execution;

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
                        currentValue = Get();
                        if (currentValue is float)
                            // promote floats to doubles
                            currentValue = Convert.ToDouble(currentValue);
                    }
                    return currentValue;
                }
                return null;
            }
            set
            {
                if (Set == null) return;
                Set(value);
            }
        }

        public void ClearCache()
        {
            currentValue = null;
        }
    }
}
