using System;

namespace kOS.Safe.Function
{
    public class FunctionAttribute : Attribute
    {
        public string[] Names { get; set; }
        public bool IsInvariant { get; set; } = false;
        public Type ReturnType
        {
            get => returnType;
            set
            {
                if (value != null && !typeof(Encapsulation.Structure).IsAssignableFrom(value))
                {
#if DEBUG
                    throw new ArgumentException($"{value} is not an accepted return type for a kOS function since it does not subclass {typeof(Encapsulation.Structure)}.");
#else
                    Utilities.SafeHouse.Logger.LogError($"The supplied type, {value}, is not an accepted return type for a kOS function since it is not a subclass of ${typeof(Encapsulation.Structure)}.");
                    returnType = null;
#endif
                }
                returnType = value;
            }
        }
        private Type returnType = null;
        public FunctionAttribute(params string[] names)
        {
            Names = names;
        }
    }
}
