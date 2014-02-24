using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Execution;

namespace kOS.Bindings
{
    public class BoundVariable : Variable
    {
        public BindingManager.BindingSetDlg Set;
        public BindingManager.BindingGetDlg Get;
        public CPU cpu;

        private object _currentValue = null;
        private bool _wasUpdated = false;

        public override object Value
        {
            get
            {
                if (Get != null)
                {
                    if (_currentValue == null)
                        _currentValue = Get(cpu);
                    return _currentValue;
                }
                else
                    return null;
            }
            set
            {
                if (Set != null)
                {
                    _currentValue = value;
                    _wasUpdated = true;
                }
            }
        }

        public void ClearValue()
        {
            _currentValue = null;
            _wasUpdated = false;
        }

        public void SaveValue()
        {
            if (_wasUpdated && _currentValue != null)
            {
                Set(cpu, _currentValue);
            }
        }
    }
}
