using System;

namespace kOS.Safe.Compilation
{
    /// <summary>
    /// This attribute is used to mark classes that may implement
    /// addition, subtraction, and negation where addition with
    /// negation are not equivalent to subtraction.
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class NegatableAttribute : Attribute
    {
        public bool IsNegatable { get; }
        public NegatableAttribute(bool isNegatable)
        {
            IsNegatable = isNegatable;
        }
    }
}
