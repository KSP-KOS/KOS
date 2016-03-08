using System;

namespace kOS.Safe.Encapsulation.Suffixes
{
    [kOS.Safe.Utilities.KOSNomenclature("ConfigKey")] // not even sure when anyone will ever see this in a script.
    public class ConfigKey : Structure
    {
        private object val;
        public string StringKey {get;private set;}
        public string Alias {get;private set;}
        public string Name {get; private set;}
        public Type ValType {get;private set;}
        public object Value {get{return val;} set{ val = SafeSetValue(value); } }
        public object MinValue {get;private set;}
        public object MaxValue {get;private set;}

        public ConfigKey(string stringKey, string alias, string name, object defaultValue, object min, object max, Type type)
        {
            StringKey = stringKey;
            Alias = alias;
            Name = name;
            val = defaultValue;
            MinValue = min;
            MaxValue = max;
            ValType = type;
        }


        /// <summary>
        /// Return the new value after it's been altered or the change was denied.
        /// </summary>
        /// <param name="newValue">attempted new value</param>
        /// <returns>new value to actually use, maybe constrained or even unchanged if the attempted value is disallowed</returns>
        private object SafeSetValue(object newValue)
        {
            object returnValue = Value;
            if (newValue==null || (! ValType.IsInstanceOfType(newValue)))
                return returnValue;

            if (Value is int)
            {
                if ((int)newValue < (int)MinValue)
                    returnValue = MinValue;
                else if ((int)newValue > (int)MaxValue)
                    returnValue = MaxValue;
                else
                    returnValue = newValue;
                
                // TODO: If and when we end up making warning-level exceptions that don't break
                // the execution but still get logged, then log such a warning here mentioning
                // if the value attempted was denied and changed if it was.
            }
            else if (Value is bool)
            {
                returnValue = newValue;
            }
            else
            {
                throw new Exception( "kOS CONFIG has new type that wasn't supported yet:  contact kOS developers" );
            }
            return returnValue;
        }
    }
}