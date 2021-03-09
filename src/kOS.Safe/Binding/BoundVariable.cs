using kOS.Safe.Execution;
using kOS.Safe.Encapsulation;

namespace kOS.Safe.Binding
{
    public class BoundVariable : Variable
    {
        public BindingSetDlg Set;
        public BindingGetDlg Get;

        public bool Volatile = false;

        private object currentValue;

        public override object Value
        {
            get
            {
                if (Get == null) return null;

                // This code used to simply elevate float variables to doubles.  With the
                // new primitive encapsulation types we instead encapsulate any value returned
                // by the delegate.  This makes it so that all of the getters for bound variables
                // don't need to be modified to explicitly return the encapsulated types.
                if (!Volatile && currentValue != null)
                    return currentValue;
                currentValue = Structure.FromPrimitive(Get());
                return currentValue;
            }
            set
            {
                if (Set == null) return;
                // By converting to the primitive value of an encapsulated type, we can avoid a clash
                // between unboxing and casting in the set delegate.  While the new encapsulated types
                // support implicit conversion to their primitive counterparts, .net and mono treat 
                // "(double)object" as an unboxing, and "(double)float" or "(double)ScalarValue" as
                // a cast.  As a result, the correct cast in the Set delegate would become
                // "(double)(ScalarValue)object" since it will unbox the object, and then cast it.
                // If the delegates supported typing, we could use "Convert" to do this unbox/conversion
                // all at the same time.  Instead, we pass the primitive value to avoid these conflicts.
                Set(Structure.ToPrimitive(value));
                // Because the value was just set, we should not assume that the cache is still valid
                ClearCache(); 
            }
        }

        public void ClearCache()
        {
            currentValue = null;
        }
    }
}
