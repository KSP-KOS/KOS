using System;

namespace kOS.Execution
{
    /// <summary>
    /// A ScopeLossObserver is any object that could be a value to
    /// a named kOS variable, which wants to be informed of the
    /// fact that the named variable referring to it has just
    /// gone away or been overwritten.  This is for objects that
    /// may have other C# references pointing to them other than
    /// the named variable and therefore would otherwise never get
    /// orphaned and never be able to detect that they are
    /// orphaned as far as the kosscript code is concerned.
    /// </summary>
    public interface ScopeLostObserver
    {
        void ScopeLost( string name ); // name is the variable that just lost the object.
    }
}
