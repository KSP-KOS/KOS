using System;

namespace kOS.Safe.Compilation
{
    /// <summary>
    /// This attribute is used to mark math operators that are
    /// irreversible.
    /// </summary>
    /// <remarks>
    /// This attribute must be applied to both operators that
    /// would normally be reversible with each other, unless the
    /// irreversability is unidirectional.
    /// </remarks>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class IrreversibleAttribute : Attribute
    {
        public bool IsIrreversible { get; }
        public IrreversibleAttribute(bool isIrreversible = true)
        {
            IsIrreversible = isIrreversible;
        }
    }
}
