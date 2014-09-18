namespace kOS.Safe.Execution
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
                if (_value is IKOSScopeObserver)
                {
                    ((IKOSScopeObserver)_value).LinkCount++;
                }
                // Alter link count of my previous value:
                if (oldValue is IKOSScopeObserver)
                {
                    ((IKOSScopeObserver)oldValue).LinkCount--;
                    
                    // If the old value no longer has any kOS Variable references, inform it:
                    if ( ((IKOSScopeObserver)oldValue).LinkCount <= 0 ) {
                        ((IKOSScopeObserver)oldValue).ScopeLost();
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
            // IKOSScopeObserver variable go out of scope without explicilty
            // cleaning it up, or because a script crashed, this last check
            // will eventually catch that fact, but it might not happen
            // immediately.  Sometimes it takes 10-20 seconds:
            if (_value is IKOSScopeObserver)
            {
                ((IKOSScopeObserver)_value).LinkCount--;
                if ( ((IKOSScopeObserver)_value).LinkCount <= 0 ) {
                    ((IKOSScopeObserver)_value).ScopeLost();
                }
            }
        }
    }
}
