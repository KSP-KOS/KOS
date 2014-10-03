using System;

namespace kOS.Safe.Exceptions
{
    /// <summary>
    /// Thrown whenever KOS compiler encounters something it does not like.
    /// This is to be distinguished from errors that occur while code is
    /// actually running.  This exception, and exceptions derived from
    /// it, might be handled differently because they are expected to
    /// occur *prior* to actually letting the CPU start executing the
    /// program's opcodes.
    /// </summary>
    public class KOSCompileException: Exception, IKOSException
    {
        // Just default the Verbose message to return the terse message:
        public virtual string VerboseMessage { get{return base.Message;} set{} }

        // Just nothing by default:
        public virtual string HelpURL { get{ return "";} set{} }

        public KOSCompileException(string message) : base(message)
        {
        }
    }
}
