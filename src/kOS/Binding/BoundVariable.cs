using System;
using kOS.Execution;
using kOS.Safe.Execution;

namespace kOS.Binding
{
    public class BoundVariable : Variable
    {
        public BindingManager.BindingSetDlg Set;
        public BindingManager.BindingGetDlg Get;
        public CPU Cpu { get; set; }

        private object currentValue;
        private bool wasUpdated;

        public override object Value
        {
            get
            {
                if (Get != null)
                {
                    if (currentValue == null)
                    {
                        currentValue = Get(Cpu);
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

                currentValue = value;
                wasUpdated = true;
            }
        }

        public void ClearValue()
        {
            currentValue = null;
            wasUpdated = false;
        }

        public void SaveValue()
        {
            if (wasUpdated && currentValue != null)
            {
                Set(Cpu, currentValue);
            }
        }
    }
}
