using System;

namespace kOS.Safe.Function
{
    public class FunctionAttribute : Attribute
    {
        public string[] Names { get; set; }
        /// <summary>
        /// Gets a value indicating whether this function's result is
        /// invariant. That is, if the value of the result can be
        /// known at compile time.
        /// </summary>
        /// <remarks>A function that is both invariant and inert can
        /// be replaced by its result at compile time.</remarks>
        /// <value>
        ///   <c>true</c> if this function is invariant; otherwise, <c>false</c>.
        /// </value>
        public bool IsInvariant { get; set; } = false;
        /// <summary>
        /// Gets a value indicating whether this function affects
        /// the wider simulation state or changes the value of an
        /// object.
        /// </summary>
        /// <remarks>A function that is both inert and invariant can
        /// be replaced by its result at compile time. A function
        /// that is inert can be relocated within a script without
        /// issue (for the same input arguments).</remarks>
        /// <value>
        ///   <c>true</c> if this function is inert; otherwise, <c>false</c>.
        /// </value>
        public bool IsInert { get; set; } = false;
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
