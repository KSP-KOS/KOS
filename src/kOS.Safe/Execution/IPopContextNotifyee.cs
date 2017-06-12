
using System;

namespace kOS.Safe.Execution
{
    /// <summary>
    /// Any object that wants to be told when the current CPU's ProgramContext
    /// ended and is about to go away (either through crashing or normal ending)
    /// needs to implement this interface, then register itself with
    /// CPU.AddPopContextNotifyee().
    /// </summary>
    public interface IPopContextNotifyee
    {
        /// <summary>
        /// Notify hook called by CPU on ending and removing a Program Context if
        /// you've registered yourself with CPU.AddPopContextNotifyee().
        /// <description>
        /// <list>
        /// <item>The timing of this hook is guaranteed to happen *after*
        /// the program context is no longer executing (no more Opcodes will
        /// happen), but just *before* it has cleared out the
        /// ProgramContext information. </item>
        /// </list>
        /// </description>
        /// </summary>
        /// <param name="context">The ProgramContext that is being
        /// terminated by the CPU and removed.</param>
        /// <returns>Retention flag.  Whether you wish this hook to
        /// remain in place to be called again on the next ProgramContext's
        /// death.  If you return true, the CPU will notify you again the
        /// next time a program context ends.  If you return false, it
        /// will not.  Returning false is probably the right thing in most
        /// cases.
        /// </returns>
        bool OnPopContext(IProgramContext context);
    }
}

