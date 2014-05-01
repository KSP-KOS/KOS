using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.Execution
{
    public class Variable
    {
        public string Name;
        private object _value;
        public virtual object Value
        {
            get { return _value; }
            set
            {
                // Value (capital 'V') is this property, while
                // value (lower 'v') is the C# setter argument
                
                // If changing the variable to a new value and the old
                // value was a ScopeLossObserver, notify it:
                if (  _value is ScopeLostObserver   &&
                    ! System.Object.ReferenceEquals(_value, value) )
                {
                    ((ScopeLostObserver)_value).ScopeLost(Name);
                }
                _value = value;
                
                // Question to @marianoapp - does the same logic have to
                // be put into BoundVariable, which overrides this?  I'm
                // not sure it does, since bound variables don't use
                // normal user-land objects.
            }
        }

        public override string ToString()
        {
            return Name;
        }

        ~Variable()
        {
            if (Value is ScopeLostObserver)
            {
                ((ScopeLostObserver)Value).ScopeLost(Name);
            }
        }
    }
}
