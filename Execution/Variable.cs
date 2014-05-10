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
                // Be very careful here to notice the difference
                // between _value and value:
                object oldValue = _value;
                
                _value = value;

                // Alter link count of my new value:
                if (_value is KOSScopeObserver)
                {
                    ((KOSScopeObserver)_value).linkCount++;
                }
                // Alter link count of my previous value:
                if (oldValue is KOSScopeObserver)
                {
                    ((KOSScopeObserver)oldValue).linkCount--;
                    
                    // If the old value no longer has any kOS Variable references, inform it:
                    if ( ((KOSScopeObserver)oldValue).linkCount <= 0 ) {
                        ((KOSScopeObserver)oldValue).ScopeLost();
                    }
                }

                // Question to @marianoapp - does the same logic have to
                // be put into BoundVariable, which overrides this?  I
                // don't think it does, since bound variables don't use
                // normal user-land objects and seem to live forever.
            }
        }

        public override string ToString()
        {
            return Name;
        }

        ~Variable()
        {
            // There is no guarantee of timeliness for calling a finalizer,
            // only that it will happen eventually before the object goes
            // away.  So if the user lets a
            // KOSScopeObserver variable go out of scope without explicilty
            // cleaning it up, or because a script crashed, this last check
            // will eventually catch that fact, but it might not happen
            // immediately.  Sometimes it takes 10-20 seconds:
            if (_value is KOSScopeObserver)
            {
                ((KOSScopeObserver)_value).linkCount--;
                if ( ((KOSScopeObserver)_value).linkCount <= 0 ) {
                    ((KOSScopeObserver)_value).ScopeLost();
                }
            }
        }
    }
}
