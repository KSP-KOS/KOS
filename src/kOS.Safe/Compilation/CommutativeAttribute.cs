using System;

namespace kOS.Safe.Compilation
{
    /// <summary>
    /// This attribute is used to mark math operators that do not
    /// follow standard commutativity rules.
    /// </summary>
    /// <remarks>
    /// Operators where the operands are different Types are
    /// assumed to be commutative in the absence of this
    /// attribute, if the operation would normally be commutative.
    /// Subtraction (a-b) is assumed to normally be commutative with
    /// negation and addition (-b+a), as well as interchangeably with
    /// addition.
    /// </remarks>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CommutativeAttribute : Attribute
    {
        public bool IsCommutative { get; }
        public CommutativeAttribute(bool isCommutative)
        {
            IsCommutative = isCommutative;
        }
    }
}
