using System;

namespace kOS.Execution
{
    /// <summary>
    /// A KOSScopeObserver is any object which wants to be kept informed
    /// of how many kOS variables there are that reference it, and
    /// wants to be informed when the kOS scope for the object
    /// is gone (meaning the last kOS variable that could have referred
    /// to it has gone away).
    /// </summary>
    // This is for objects that
    // may have other C# references pointing to them other than
    // the named variable and therefore would otherwise never get
    // orphaned and never be able to detect that they are
    // orphaned as far as the kosscript code is concerned.
    //
    public interface KOSScopeObserver
    {
        /// <summary>
        /// Updated whenever a new Variable object holding this object
        /// is made or destroyed.
        /// </summary>
        int linkCount { get; set; } 

        /// <summary>
        /// Called by the Variable object that holds this object,
        /// when the link count as far hits zero.
        /// (the last kos variable to refer to it is gone)
        /// </summary>
        void ScopeLost(); 

    }
}
